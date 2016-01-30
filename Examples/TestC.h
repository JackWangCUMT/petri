#include "Runtime/C/Petri.h"
#include <stdio.h>
#include <inttypes.h>

typedef enum ActionResult ActionResult;

static ActionResult action1() {
	printf("Action1!\n");
	return 0;
}

static ActionResult action2() {
        printf("Action2!\n");
        return 0;
}

static bool condition1(Petri_actionResult_t result) {
        printf("Condition1!\n");
        return true;
}

static ActionResult outputVar(int64_t value) {
        printf("Value: %"PRIu64"\n", value);
        return 0;
}

