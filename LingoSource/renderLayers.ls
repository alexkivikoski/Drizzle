global c, gLOprops
global gExport_finalDecalImage, gExport_finalImage, gExport_rainBowMask, gExport_fogImage, gExport_dpImage, gExport_flattenedGradientA, gExport_flattenedGradientB

on exitFrame(me)
  if (checkMinimize()) then
    _player.appMinimize()
  end if
  if (checkExit()) then
    _player.quit()
  end if
  if (checkExitRender()) then
    _movie.go(9)
  end if
  cols: number = gLOprops.size.loch * 20
  row: number = gLOprops.size.locv * 20
  repeat with q = 0 to 29
    strq = string(q)
    member("layer" & strq).image = image(cols, row, 32)
    member("gradientA" & strq).image = image(cols, row, 16)
    member("gradientB" & strq).image = image(cols, row, 16)
    member("layer" & strq & "dc").image = image(cols, row, 32)
    member("rainBowMask"& strq).image = image(cols, row, 32)
    gExport_rainBowMask[q+1] = member("rainBowMask"& strq).image
    member("finalImage"& strq).image = image(cols, row, 32)
    member("finalDecalImage"& strq).image = image(cols, row, 32)
    member("shadowImage"& strq).image = image(cols, row, 32)
    member("flattenedGradientA"& strq).image = image(cols, row, 32)
    gExport_flattenedGradientA[q+1] = member("flattenedGradientA"& strq).image
    member("flattenedGradientB"& strq).image = image(cols, row, 32)
    gExport_flattenedGradientB[q+1] = member("flattenedGradientB"& strq).image
    member("fogImage"& strq).image = image(cols, row, 32)
    member("dpImage"& strq).image = image(cols, row, 32)
  end repeat
  member("rainBowMask").image = image(cols, row, 32)
  renderLevel()
  c = 1
end