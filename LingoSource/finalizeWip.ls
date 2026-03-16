global gDisableLights, gLEProps, c, keepLooping, gCustomColor, gLoadedName, gLOprops, dptsL, fogDptsL, gCameraProps, gCurrentRenderCamera, gRenderCameraPixelPos, gRenderCameraTilePos, DRPxl, gGradientImages, gAnyDecals, gDecalColors

on exitFrame me
  type cols: number
  type rows: number
  type extrarect: rect
  type extrapoint: point
  type lightmargin: number
  type dp: number
  type pstrcti: rect
  type pstrct: rect
  type inv: image
  type smpl: image
  type smpl2: image
  type smplps: number
  type l: string
  type lr: number
  type getrecti: rect
  type getrect: rect
    type gRenderCameraPixelPosi: point
  
  if checkMinimize() then
    _player.appMinimize() 
  end if
  if checkExit() then
    _player.quit()
  end if
  if checkExitRender() then
    _movie.go(9)
  end if
  
  
  
  cols = gLOprops.size.loch
  rows = gLOprops.size.locv
  --cols = 100
  --rows = 60
  
  member("finalImage").image = image(cols*20, rows*20, 32)
  if not gDisableLights then
    member("shadowImage").image  = image(cols*20, rows*20, 32)
  end if
  member("finalDecalImage").image  = image(cols*20, rows*20, 32)
  gDecalColors = []
  
  extrarect = rect(0, 0, 0, 0)
  extraPoint = point(extrarect.right, extrarect.bottom)
  lightmargin = 0
  
  gRenderCameraPixelPosi = point(0*20, 0*20)
  gRenderCameraPixelPos = gRenderCameraPixelPos + gRenderCameraPixelPosi
  
  member("dumpImage").image = image((cols*20) + lightmargin*2, (rows*20) + lightmargin*2, 32)
  
  repeat with q = 1 to 30 then
    --member("finalImage"&string(30-q)).image = image(cols*20, rows*20, 32)
    --member("shadowImage"&string(30-q)).image  = image(cols*20, rows*20, 32)
    --member("finalDecalImage"&string(30-q)).image  = image(cols*20, rows*20, 32)

    dp = 30-q-5
    
    member("dumpImage").image.copyPixels(member("layer"&string(30-q)).image, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin), rect(0,0,cols*20,rows*20))
    member("layer"&string(30-q)).image.copyPixels( member("dumpImage").image, member("dumpImage").image.rect, member("dumpImage").image.rect)
    if not gDisableLights then
      pstRcti = rect(point(0,0)-extraPoint,point(cols*20,rows*20)+extraPoint)
      member("shadowImage"&string(30-q)).image.copyPixels(member("layer"&string(30-q)).image, pstRcti, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+extrarect, {#ink:36, #color:color(255,255,255)})
      member("shadowImage"&string(30-q)).image.copyPixels(member("layer"&string(30-q)&"sh").image, pstRcti, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+extrarect, {#ink:36})
      pstRct = rect(depthPnt(point(0,0)-extraPoint,dp),depthPnt(point(cols*20,rows*20)+extraPoint,dp))
      member("shadowImage").image.copyPixels(member("layer"&string(30-q)).image, pstRct, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+extrarect, {#ink:36, #color:color(255,255,255)})
      member("shadowImage").image.copyPixels(member("layer"&string(30-q)&"sh").image, pstRct, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+extrarect, {#ink:36})

      inv = image((cols*20), (rows*20), 1)
      inv.copyPixels(member("pxl").image, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#color:255})
      inv.copyPixels(member("shadowImage"&string(30-q)).image, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
      member("shadowImage"&string(30-q)).image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20))
    end if

    --member("fogImage"&string(30-q)).image = image(cols*20,rows*20,#L8)

    --member("dpImage"&string(30-q)).image = image(cols*20,rows*20,#L8)
    --member("dpImage"&string(30-q)).image.copyPixels(member("pxl").image, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#color:255})
  end repeat
  
  if not gDisableLights then
    inv = image(cols*20, rows*20, 1)
    inv.copyPixels(DRPxl, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#color:255})
    inv.copyPixels(member("shadowImage").image, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
    member("shadowImage").image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20))
  end if
  
  
  
  member("fogImage").image = image(cols*20,rows*20,32)
  
  member("dpImage").image = image(cols*20,rows*20,32)
  member("dpImage").image.copyPixels(DRPxl, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#color:255})
  
  smpl = image(4,1,32)
  smpl2 = image(30, 1, 32)
  smplPs = 0
  dptsL = []
  
  fogDptsL = []
  
  repeat with q = 1 to 30 then
    dp = 30-q-5
    
    
    
    pstRcti = rect(point(0,0)-extraPoint,point(cols*20,rows*20)+extraPoint)
    repeat with q2 = 1 to q then
      member("dpImage"&string(30-q2)).image.copyPixels(member("layer"&string(30-q)).image, pstRcti, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+extrarect, {#ink:36, #color:color(255,255,255)})
    end repeat
    pstRct = rect(depthPnt(point(0,0)-extraPoint,dp),depthPnt(point(cols*20,rows*20)+extraPoint,dp))
    member("dpImage").image.copyPixels(member("layer"&string(30-q)).image, pstRct, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+extrarect, {#ink:36, #color:color(255,255,255)})
    smpl.copyPixels(DRPxl, rect(smplPs,0,4,1), rect(0,0,1,1), {#color:0})
    
    if (dp+5=12)or(dp+5=8)or(dp+5=4)then
      smpl.copyPixels(DRPxl, rect(0,0,4,1), rect(0,0,1,1), {#blend:10, #color:255})
      smplPs = smplPs + 1
        repeat with q2 = 1 to q then
        member("dpImage"&string(30-q2)).image.copyPixels(DRPxl, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#blend:10, #color:255})
      end repeat
      member("dpImage").image.copyPixels(DRPxl, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#blend:10, #color:255})
    end if
    
    repeat with q2 = 1 to q then
      member("fogImage"&string(30-q2)).image.copyPixels(member("layer"&string(30-q)).image, pstRcti, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+extrarect, {#ink:36, #color:color(255,255,255)})
      member("fogImage"&string(30-q2)).image.copyPixels(DRPxl, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#blend:5, #color:255})
    end repeat
    member("fogImage").image.copyPixels(member("layer"&string(30-q)).image, pstRct, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+extrarect, {#ink:36, #color:color(255,255,255)})
    member("fogImage").image.copyPixels(DRPxl, rect(0,0,cols*20,rows*20), rect(0,0,1,1), {#blend:5, #color:255})
    smpl2.setPixel(q-1, 0, color(255, 255, 255))
    smpl2.copyPixels(DRPxl, rect(0,0,30,1), rect(0,0,1,1), {#blend:5, #color:255})
  end repeat
  
  
  repeat with q = 1 to 4 then
    dptsL.add(smpl.getPixel(4-q, 0))
  end repeat
  
  repeat with q = 1 to 30 then
    fogDptsL.add(smpl2.getPixel(30-q, 0))
  end repeat

  repeat with q2 = 1 to 25 then
    dp = 30-q2-5
    --pstRcti = rect(point(0,0)-extraPoint,point(cols*20,rows*20)+extraPoint)
    --member("finalImage"&string(30-q2)).image.copyPixels(member("layer"&string(30-q2)).image, pstRcti, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+extrarect, {#ink:36})
    pstRct = rect(depthPnt(point(0,0)-extraPoint,dp),depthPnt(point(cols*20,rows*20)+extraPoint,dp))
    member("finalImage").image.copyPixels(member("layer"&string(30-q2)).image, pstRct, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+extrarect, {#ink:36})
    if 30-q2 = 10 then
      inv = makeSilhoutteFromImg(member("finalImage"&string(30-q2)).image, 1)
      repeat with q = 1 to gLOprops.size.loch then
        repeat with c = 1 to gLOprops.size.locv then
          if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)and(gLEProps.matrix[q][c][1][1]=0)and(gLEProps.matrix[q][c][2][1]=1) then
            pasteShortCutHole("finalImage"&string(30-q2), point(q,c), 5, "BORDER")
            pasteShortCutHole("finalImage"&string(30-q2), point(q,c), 5, color(51,10,0))
          end if
        end repeat
      end repeat
      member("finalImage"&string(30-q2)).image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})

      inv = makeSilhoutteFromImg(member("finalImage").image, 1)
      repeat with q = 1 to gLOprops.size.loch then
        repeat with c = 1 to gLOprops.size.locv then
          if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)and(gLEProps.matrix[q][c][1][1]=0)and(gLEProps.matrix[q][c][2][1]=1) then
            pasteShortCutHole("finalImage", point(q,c), 5, "BORDER")
            pasteShortCutHole("finalImage", point(q,c), 5, color(51,10,0))
          end if
        end repeat
      end repeat 
      member("finalImage").image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
      
    else if 30-q2 = 20 then
      
      inv = makeSilhoutteFromImg(member("finalImage"&string(30-q2)).image, 1)
      repeat with q = 1 to gLOprops.size.loch then
        repeat with c = 1 to gLOprops.size.locv then
          if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)and(gLEProps.matrix[q][c][1][1]=0)and(gLEProps.matrix[q][c][2][1]=0)and(gLEProps.matrix[q][c][3][1]=1) then
            pasteShortCutHole("finalImage"&string(30-q2), point(q,c), 15, "BORDER")
            pasteShortCutHole("finalImage"&string(30-q2), point(q,c), 15, color(41,9,0))
          end if
        end repeat
      end repeat
      member("finalImage"&string(30-q2)).image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
      
      inv = makeSilhoutteFromImg(member("finalImage").image, 1)
      repeat with q = 1 to gLOprops.size.loch then
        repeat with c = 1 to gLOprops.size.locv then
          if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)and(gLEProps.matrix[q][c][1][1]=0)and(gLEProps.matrix[q][c][2][1]=0)and(gLEProps.matrix[q][c][3][1]=1) then
            pasteShortCutHole("finalImage", point(q,c), 15, "BORDER")
            pasteShortCutHole("finalImage", point(q,c), 15, color(41,9,0))
          end if
        end repeat
      end repeat 
      member("finalImage").image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
      
    end if
  end repeat
  
  
  repeat with q = 25 to 30 then
    dp = 30-q-5
    pstRcti = rect(point(0,0)-extraPoint,point(cols*20,rows*20)+extraPoint)
    member("finalImage"&string(30-q)).image.copyPixels(member("layer"&string(30-q)).image, pstRcti, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+extrarect, {#ink:36})
    pstRct = rect(depthPnt(point(0,0)-extraPoint,dp),depthPnt(point(cols*20,rows*20)+extraPoint,dp))
    member("finalImage").image.copyPixels(member("layer"&string(30-q)).image, pstRct, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+extrarect, {#ink:36})
  end repeat
  
  
  repeat with q2 = 1 to 30 then
    inv = makeSilhoutteFromImg(member("finalImage"&string(30-q2)).image, 1)
    repeat with q = 1 to gLOprops.size.loch then
      repeat with c = 1 to gLOprops.size.locv then
        if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)and(gLEProps.matrix[q][c][1][1]=1) then
          pasteShortCutHole("finalImage"&string(30-q2), point(q,c), -5, "BORDER")
          pasteShortCutHole("finalImage"&string(30-q2), point(q,c), -5, color(31,8,0))
        end if
      end repeat
    end repeat
    member("finalImage"&string(30-q2)).image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
  end repeat
  
  inv = makeSilhoutteFromImg(member("finalImage").image, 1)
  repeat with q = 1 to gLOprops.size.loch then
    repeat with c = 1 to gLOprops.size.locv then
      if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)and(gLEProps.matrix[q][c][1][1]=1) then
        pasteShortCutHole("finalImage", point(q,c), -5, "BORDER")
        pasteShortCutHole("finalImage", point(q,c), -5, color(31,8,0))
      end if
    end repeat
  end repeat 
  member("finalImage").image.copyPixels(inv, rect(0,0,cols*20,rows*20), rect(0,0,cols*20,rows*20), {#ink:36, #color:color(255,255,255)})
  
  
  
  member("rainBowMask").image = image(cols*20, rows*20, 1)
  repeat with q = 1 to 30 then
    member("rainBowMask"&string(30-q)).image = image(cols*20, rows*20, 1)
  end repeat
  repeat with L in ["A", "B"] then
    member("flattenedGradient" & L).image = image(cols*20, rows*20, 16)
    repeat with bd = 0 to 29 then
      lr = 29-bd
      dp = lr-5
      member("flattenedGradient" & L & string(lr)).image = image(cols*20, rows*20, 16)
      
      member("dumpImage").image.copyPixels(member("gradient" & L & string(lr)).image, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin), rect(0,0,cols*20,rows*20))
      pstRcti = rect(point(0,0)-extraPoint,point(cols*20,rows*20)+extraPoint)
      pstRct = rect(depthPnt(point(0,0)-extraPoint,dp),depthPnt(point(cols*20,rows*20)+extraPoint,dp))
      getRecti = rect(0,0,cols*20,rows*20)+rect(gRenderCameraPixelPosi, gRenderCameraPixelPosi)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+extrarect
      getRect = rect(0,0,cols*20,rows*20)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+extrarect
      
      member("flattenedGradient" & L).image.copyPixels(member("dumpImage").image, pstRct, getRect, {#maskImage:makeSilhoutteFromImg(member("layer" & lr).image, 0).createMask()})
      
      member("flattenedGradient" & L).image.setPixel(0,0, color(0,0,0))
      member("flattenedGradient" & L).image.setPixel(cols*20-1,rows*20-1, color(0,0,0))
    end repeat
    
  end repeat
  
  if(gAnyDecals)then
    repeat with bd = 0 to 29 then
      lr = 29-bd
      dp = lr-5
      member("dumpImage").image.copyPixels(member("layer" & string(lr) & "dc").image, rect(0,0,cols*20,rows*20)+rect(lightmargin,lightmargin,lightmargin,lightmargin), rect(0,0,cols*20,rows*20))
      pstRct = rect(depthPnt(point(0,0)-extraPoint,dp),depthPnt(point(cols*20,rows*20)+extraPoint,dp))
      getRect = rect(0,0,cols*20,rows*20)+rect(gRenderCameraPixelPos, gRenderCameraPixelPos)+rect(lightmargin,lightmargin,lightmargin,lightmargin)+extrarect
      
      member("finalDecalImage").image.copyPixels(member("dumpImage").image, pstRct, getRect, {#maskImage:makeSilhoutteFromImg(member("layer" & lr).image, 0).createMask()})
      
      member("finalDecalImage").image.setPixel(0,0, color(0,0,0))
      member("finalDecalImage").image.setPixel(cols*20-1,rows*20-1, color(0,0,0))
    end repeat
  end if
  c = 1
  global gLevel
  if gLevel.lightType = "No Light" and not gDisableLights then
    member("shadowImage").image.copyPixels(DRPxl, member("shadowImage").image.rect, DRPxl.rect)
  end if
  
  keepLooping = 1
  
end