#include "Runtime/Cpp/Petri.h"
using Petri::ActionResult;

class TestNS {
public:
	static ActionResult action1() {
		std::cout << "Action1!" << std::endl;
		return {};
	}

        static ActionResult action2() {
                std::cout << "Action2!" << std::endl;
                return {};
        }

        static bool condition1(ActionResult result) {
                std::cout << "Condition1!" << std::endl;
                return true;
        }

	static ActionResult outputVar(int64_t value) {
		std::cout << "Value: " << value << std::endl;
		return {};
	}
};

