#!/bin/sh
# Wait for another Unity to let go of a project before we open it ourselves.
#
# Unity holds <project>/Temp/UnityLockfile with flock. Asking the kernel about that lock beats
# matching process names: a holder that died has already let go, so there is no stale file to clean.
#
# usage: unity-lock.sh <project dir> [seconds to wait]
set -eu

project=${1:?usage: unity-lock.sh <project dir> [seconds]}
limit=${2:-900}
lock="$project/Temp/UnityLockfile"
poll=2
report=30

# open read-only so a missing file is never created here, then ask for the lock without blocking.
# the subshell matters: a bare exec redirection would outlive the function it ran in.
held() {
    [ -r "$lock" ] || return 1
    ( exec 9<"$lock" && flock -n 9 ) 2>/dev/null && return 1
    return 0
}

# who has it and how long they have been at it. several processes can hold the lockfile open (a
# wrapper and its child), and this never fails: losing the message must not lose the wait
holder() {
    pid=$(fuser "$lock" 2>/dev/null | awk '{ print $1 }') || pid=""
    if [ -z "$pid" ]; then
        echo "an instance that is already going"
        return 0
    fi
    line=$(ps -o etime=,args= -p "$pid" 2>/dev/null) || line=""
    if [ -z "$line" ]; then
        echo "pid $pid"
        return 0
    fi
    echo "$line" | awk -v p="$pid" '{ t=$1; $1=""; sub(/^ +/,""); printf "pid %s, up %s: %.70s", p, t, $0 }'
}

held || exit 0

echo "unity: project is open elsewhere ($(holder))"
echo "unity: waiting up to ${limit}s for it to finish"

waited=0
while held; do
    if [ "$waited" -ge "$limit" ]; then
        echo "unity: gave up after ${waited}s, still held by $(holder)" >&2
        exit 1
    fi
    sleep "$poll"
    waited=$((waited + poll))
    # a long build is not a wedged one, so say we are still here rather than going quiet
    if [ $((waited % report)) -eq 0 ]; then
        echo "unity: still waiting (${waited}s)"
    fi
done

echo "unity: project free after ${waited}s, carrying on"
