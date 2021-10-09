# Paths - small helpers so I can build/run/test without opening the editor each time.
# UNITY has to point at the 2020.3 editor binary; that path is machine specific so
# set it in local.mk (not committed) rather than here.
UNITY ?= unity
PROJECT := Paths
PLAYER := Build/Linux/Paths
# Unity stamps the launcher with the template's mtime, not the build time, so we
# can't use it as the make target - touch our own stamp once a build succeeds.
STAMP := Build/.built

# machine-local overrides (the UNITY path, mostly)
-include local.mk

# rebuild the player only when something actually changed. skip TextMesh Pro - it's
# vendored, never changes, and the space in the path trips up make's prereq parsing.
SOURCES := $(shell find $(PROJECT)/Assets -type f \( -name '*.cs' -o -name '*.shader' -o -name '*.unity' \) -not -path '*/TextMesh Pro/*')

.PHONY: demo build run compile test open clean

# build if stale, then run the demo
demo: $(STAMP)
	./$(PLAYER)

build: $(STAMP)

$(STAMP): $(SOURCES)
	$(UNITY) -batchmode -nographics -projectPath $(PROJECT) -executeMethod Editor.Build.Linux -logFile -
	@touch $(STAMP)

run:
	./$(PLAYER)

# quick green/red compile check, no build
compile:
	$(UNITY) -batchmode -nographics -quit -projectPath $(PROJECT) -logFile -

test:
	$(UNITY) -batchmode -nographics -quit -projectPath $(PROJECT) -runTests -testPlatform EditMode -testResults Build/results.xml -logFile -

open:
	$(UNITY) -projectPath $(PROJECT)

clean:
	rm -rf Build
