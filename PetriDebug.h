//
//  PetriDebug.h
//  IA Pétri
//
//  Created by Rémi on 22/11/2014.
//

#ifndef IA_Pe_tri_PetriDebug_h
#define IA_Pe_tri_PetriDebug_h

#include "Petri.h"
#include "Socket.h"

class PetriDebug : public PetriNet {
public:
	PetriDebug(std::uint16_t port, std::string host) : PetriNet(), _socket(SOCK_TCP), _port(port), _host(host) {}

	virtual ~PetriDebug() = default;

	virtual void run() override {
		if(!_socket.connect(_host.c_str(), _port))
			throw std::runtime_error("Unable to connect to the debugging server");
		_serverCommunication = std::thread(&PetriDebug::serverCommunication, this);
	}

	virtual void stop() override {
		_isDebugging = false;
		_serverCommunication.join();
	}

protected:
	void serverCommunication() {
		while(_isDebugging && _socket.getState() == SOCK_CONNECTED) {
			
		}

		_socket.shutDown();
		_isDebugging = false;
		this->PetriNet::stop();
	}

	Socket _socket;
	std::uint16_t _port;
	std::string _host;
	std::thread _serverCommunication;
	std::atomic_bool _isDebugging = {false};
};

#endif
