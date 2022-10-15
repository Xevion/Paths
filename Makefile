# Paths - small helpers so I can build/run/test without opening the editor each time.
# UNITY has to point at the 2020.3 editor binary; that path is machine specific so
# set it in local.mk (not committed) rather than here.
UNITY ?= unity
DOTNET ?= dotnet
PROJECT := app
PLAYER := Build/Linux/Paths
WEB := Build/WebGL
PORT ?= 8000
# Unity stamps the launcher with the template's mtime, not the build time, so we
# can't use it as the make target - touch our own stamp once a build succeeds.
STAMP := Build/.built
WEBSTAMP := Build/.web

# machine-local overrides (the UNITY path, mostly)
-include local.mk

# the editor spews a novel to stdout, so log it to a file and just print pass/fail.
# VERBOSE=1 streams it instead when you actually want to watch.
LOG := Build/unity.log
# unity resolves -testResults against the project folder, not the cwd, so spell it out
PLAYRESULTS := $(CURDIR)/Build/playmode.xml
ifeq ($(VERBOSE),1)
LOGARG := -logFile -
else
LOGARG := -logFile $(LOG)
endif

# rebuild the player only when something actually changed. skip TextMesh Pro - it's
# vendored, never changes, and the space in the path trips up make's prereq parsing.
SOURCES := $(shell find $(PROJECT)/Assets -type f \( -name '*.cs' -o -name '*.shader' -o -name '*.unity' \) -not -path '*/TextMesh Pro/*')

.PHONY: demo build run play web compile test test-play bench open clean

# build if stale, then run the demo
demo: $(STAMP)
	./$(PLAYER)

# build the WebGL demo if stale, then serve it (WebGL can't run off file://).
# serve.py sends the gzip headers the build needs; plain http.server won't.
web: $(WEBSTAMP)
	@echo "serving $(WEB) at http://localhost:$(PORT) - ctrl-c to stop"
	cd $(WEB) && python3 ../../tools/serve.py $(PORT)

build: $(STAMP)

# like demo, but small window + player log to a file so I can watch what it's doing
play: $(STAMP)
	./$(PLAYER) -screen-fullscreen 0 -screen-width 1280 -screen-height 800 -logfile Build/player.log

$(STAMP): $(SOURCES)
	@mkdir -p Build
	@$(UNITY) -batchmode -nographics -projectPath $(PROJECT) -executeMethod Editor.Build.Linux $(LOGARG) \
		&& touch $(STAMP) && echo "build OK -> $(PLAYER)" \
		|| (echo "build FAILED (see $(LOG)):"; grep -snE 'error CS|: error |BuildFailedException' $(LOG) | head -40; false)

$(WEBSTAMP): $(SOURCES)
	@mkdir -p Build
	@$(UNITY) -batchmode -nographics -projectPath $(PROJECT) -executeMethod Editor.Build.WebGL $(LOGARG) \
		&& touch $(WEBSTAMP) && echo "web build OK -> $(WEB)" \
		|| (echo "web build FAILED (see $(LOG)):"; grep -snE 'error CS|: error |BuildFailedException' $(LOG) | head -40; false)

run:
	./$(PLAYER)

# quick green/red compile check, no build
compile:
	@mkdir -p Build
	@$(UNITY) -batchmode -nographics -quit -projectPath $(PROJECT) $(LOGARG) \
		&& echo "compile OK" \
		|| (echo "compile FAILED (see $(LOG)):"; grep -snE 'error CS|: error |Compilation failed' $(LOG) | head -40; false)

# the core tests build against packages/core directly, so they run here in a couple of seconds
# instead of waiting on an editor domain reload.
test:
	$(DOTNET) test Paths.sln --nologo

# the interface tests need a real event system and a real grid, so they run in the editor.
# no -nographics here: the ui raycasts want a display.
test-play:
	@mkdir -p Build
	@$(UNITY) -batchmode -projectPath $(PROJECT) -runTests -testPlatform PlayMode \
		-testResults $(PLAYRESULTS) $(LOGARG) \
		&& echo "playmode tests OK" \
		|| (echo "playmode tests FAILED (see $(PLAYRESULTS)):"; \
		    grep -soE 'name="[A-Za-z]+"[^>]*result="Failed"' $(PLAYRESULTS); false)

# release only - a debug build's numbers are meaningless. BENCH=<filter> to run just one.
bench:
	$(DOTNET) run -c Release --project bench/Paths.Benchmarks -- --filter '$(or $(BENCH),*)'

open:
	$(UNITY) -projectPath $(PROJECT)

clean:
	rm -rf Build
