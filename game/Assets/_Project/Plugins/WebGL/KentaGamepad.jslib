// Manettes de la borne lues directement dans l'API Gamepad du navigateur (build Web seulement).
// Unity 6.3 ne voit, en Web, que la manette à l'index 0 de navigator.getGamepads() ; sur la borne, le joueur 1 peut
// être à un autre index. Ici : les manettes branchées, triées par index ; le joueur 1 = la première, le joueur 2 = la
// deuxième. Même lecture que le menu de la borne (anatidae-arcade) : buttons[0..6] = actions, buttons[8] = bouton blanc,
// axes[0] = horizontal, axes[1] = vertical (haut = +1 sur la borne).
mergeInto(LibraryManager.library, {
  KentaPad_Poll: function () {
    var list = [];
    try {
      var pads = navigator.getGamepads ? navigator.getGamepads() : [];
      for (var i = 0; i < pads.length; i++)
        if (pads[i] && pads[i].connected !== false) list.push(pads[i]);
    } catch (e) { }
    list.sort(function (a, b) { return a.index - b.index; });
    Module.kentaPads = list;
    return list.length;
  },

  KentaPad_Button: function (slot, button) {
    var p = Module.kentaPads && Module.kentaPads[slot];
    if (!p || button >= p.buttons.length) return 0;
    var b = p.buttons[button];
    return (typeof b === "object" ? (b.pressed || b.value > 0.5) : b > 0.5) ? 1 : 0;
  },

  KentaPad_Axis: function (slot, axis) {
    var p = Module.kentaPads && Module.kentaPads[slot];
    if (!p || axis >= p.axes.length) return 0;
    return p.axes[axis] || 0;
  },

  KentaPad_Index: function (slot) {
    var p = Module.kentaPads && Module.kentaPads[slot];
    return p ? p.index : -1;
  },

  KentaPad_Id: function (slot) {
    var p = Module.kentaPads && Module.kentaPads[slot];
    var s = p ? (p.id || "") : "";
    var size = lengthBytesUTF8(s) + 1;
    var buffer = _malloc(size);
    stringToUTF8(s, buffer, size);
    return buffer;
  }
});
