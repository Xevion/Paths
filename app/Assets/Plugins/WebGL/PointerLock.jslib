// Unity asks for a pointer lock from the animation frame loop, so its request is deferred to
// whatever event comes next and refused. Arming our own mousedown listener puts it in the gesture.
mergeInto(LibraryManager.library, {
  PathsArmPointerLock: function (armed) {
    var canvas = Module.canvas;
    if (!canvas) return;

    if (!canvas.pathsLockHooked) {
      canvas.pathsLockHooked = true;
      canvas.addEventListener('mousedown', function () {
        if (!canvas.pathsLockArmed) return;
        var request = canvas.requestPointerLock || canvas.mozRequestPointerLock ||
                      canvas.webkitRequestPointerLock;
        if (request) request.call(canvas);
      });
    }

    canvas.pathsLockArmed = !!armed;
  },

  // arming is the hover's business, so this only drops the lock. the cursor warps back onto the
  // field it came from, which fires no fresh enter, and the next press has to grab again.
  PathsExitPointerLock: function () {
    var exit = document.exitPointerLock || document.mozExitPointerLock ||
               document.webkitExitPointerLock;
    if (exit) exit.call(document);
  }
});
