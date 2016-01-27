/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

//
//  PetriNet.cpp
//  Pétri
//
//  Created by Rémi on 29/11/2014.
//

#include "PetriNet.h"
#include "PetriNetImpl.h"
#include "lock.h"

namespace Petri {

    PetriNet::PetriNet(std::string const &name)
            : PetriNet(std::make_unique<Internals>(*this, name)) {}
    PetriNet::PetriNet(std::unique_ptr<Internals> internals)
            : _internals(std::move(internals)) {}

    PetriNet::~PetriNet() {
        this->stop();
    }

    Action &PetriNet::addAction(Action action, bool active) {
        if(this->running()) {
            throw std::runtime_error("Cannot modify running state chart!");
        }

        _internals->_states.emplace_back(std::move(action), active);

        return _internals->_states.back().first;
    }

    std::string const &PetriNet::name() const {
        return _internals->_name;
    }

    bool PetriNet::running() const {
        return _internals->_running;
    }

    void PetriNet::addVariable(std::uint_fast32_t id) {
        _internals->_variables.emplace(std::make_pair(id, std::make_unique<Atomic>()));
    }

    Atomic &PetriNet::getVariable(std::uint_fast32_t id) {
        auto it = _internals->_variables.find(id);
        if(it == _internals->_variables.end()) {
            throw std::runtime_error("Non existing variable requested: " + std::to_string(id));
        }
        return *it->second;
    }

    void PetriNet::run() {
        if(this->running()) {
            throw std::runtime_error("Already running!");
        }

        for(auto &p : _internals->_states) {
            if(p.second) {
                _internals->_running = true;
                _internals->enableState(p.first);
            }
        }
    }

    void PetriNet::stop() {
        if(this->running()) {
            _internals->_running = false;
            _internals->_activationCondition.notify_all();
        }
        _internals->_actionsPool.cancel();
    }

    void PetriNet::join() {
        // Quick and dirty…
        while(this->running()) {
            std::this_thread::sleep_for(std::chrono::nanoseconds(1'000'000));
        }
    }

    void PetriNet::Internals::executeState(Action &state) {
        Action *nextState = nullptr;

        actionResult_t res;

        {
            std::vector<std::unique_lock<std::mutex>> locks;
            locks.reserve(state.getVariables().size());
            for(auto &var : state.getVariables()) {
                locks.emplace_back(_this.getVariable(var).getLock());
            }

            lock(locks.begin(), locks.end());

            // Runs the Callable
            res = state.action()(_this);
        }

        if(!state.transitions().empty()) {
            std::list<Transition *> transitionsToTest;
            for(auto &t : state.transitions()) {
                transitionsToTest.push_back(const_cast<Transition *>(&t));
            }

            auto lastTest = ClockType::time_point();

            while(_running && transitionsToTest.size()) {
                auto now = ClockType::now();
                auto minDelay = ClockType::duration::max() / 2;

                for(auto it = transitionsToTest.begin(); it != transitionsToTest.end();) {
                    bool isFulfilled = false;

                    if((now - lastTest) >= (*it)->delayBetweenEvaluation()) {
                        // Testing the transition
                        {
                            std::vector<std::unique_lock<std::mutex>> locks;
                            locks.reserve((*it)->getVariables().size());
                            for(auto &var : (*it)->getVariables()) {
                                locks.emplace_back(_this.getVariable(var).getLock());
                            }

                            lock(locks.begin(), locks.end());

                            isFulfilled = (*it)->isFulfilled(res);
                        }

                        minDelay = std::min(minDelay, (*it)->delayBetweenEvaluation());
                    } else {
                        minDelay = std::min(minDelay, (*it)->delayBetweenEvaluation() - (now - lastTest));
                    }

                    if(isFulfilled) {
                        Action &a = (*it)->next();
                        std::lock_guard<std::mutex> tokensLock(a.tokensMutex());
                        if(++a.currentTokensRef() >= a.requiredTokens()) {
                            a.currentTokensRef() -= a.requiredTokens();

                            if(nextState == nullptr) {
                                nextState = &a;
                            } else {
                                this->enableState(a);
                            }
                        }

                        it = transitionsToTest.erase(it);
                    } else {
                        ++it;
                    }
                }

                if(nextState != nullptr) {
                    break;
                } else {
                    lastTest = now;

                    while(ClockType::now() - lastTest <= minDelay) {
                        std::this_thread::sleep_for(std::min(1000000ns, minDelay));
                    }
                }
            }
        }

        if(nextState != nullptr) {
            this->swapStates(state, *nextState);
        } else {
            this->disableState(state);
        }
    }

    void PetriNet::Internals::swapStates(Action &oldAction, Action &newAction) {
        {
            std::lock_guard<std::mutex> lk(_activationMutex);
            _activeStates.insert(&newAction);

            auto it = _activeStates.find(&oldAction);
            assert(it != _activeStates.end());
            _activeStates.erase(it);
        }

        this->stateDisabled(oldAction);
        this->stateEnabled(newAction);

        _actionsPool.addTask(make_callable([this, &newAction]() { this->executeState(newAction); }));
    }

    void PetriNet::Internals::enableState(Action &a) {
        {
            std::lock_guard<std::mutex> lk(_activationMutex);
            _activeStates.insert(&a);

            if(_actionsPool.threadCount() < _activeStates.size()) {
                _actionsPool.addThread();
            }
        }

        this->stateEnabled(a);
        _actionsPool.addTask(make_callable([this, &a]() { this->executeState(a); }));
    }

    void PetriNet::Internals::disableState(Action &a) {
        std::lock_guard<std::mutex> lk(_activationMutex);

        auto it = _activeStates.find(&a);
        assert(it != _activeStates.end());

        _activeStates.erase(it);

        this->stateDisabled(a);
        if(_activeStates.size() == 0 && _running) {
            _this.stop();
        }
    }
}
