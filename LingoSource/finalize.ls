global gLEProps, c, keepLooping, gCustomColor, gLoadedName, gLOprops, dptsL, fogDptsL, gCameraProps, gCurrentRenderCamera, gRenderCameraPixelPos, gRenderCameraTilePos, DRPxl, gGradientImages, gAnyDecals, gDecalColors
global gExport_finalDecalImage, gExport_finalImage, gExport_rainBowMask, gExport_fogImage, gExport_dpImage, gExport_flattenedGradientA, gExport_flattenedGradientB
global gDisableLights, gDisablePerspective, gExportEnabled, gLevel, gSmpl, gSmpl2, gSmplPs
global grimeActive, grimeOnGradients, bkgFix, gPEcolors
global DRFinalImage, DRFogImage, DRDpImage, DRShadowImage, DRRainbowMask, DRFlattenedGradientA, DRFlattenedGradientB, DRFinalDecalImage

on startFrame (me)
  type inv: image
  type pstrct: rect

  cols = gLOprops.size.loch
  rows = gLOprops.size.locv

  pstRct = rect(point(0,0),point(cols*20,rows*20))
  member("finalImage").image = image(cols*20, rows*20, 32)
  if not gDisableLights then
    member("shadowImage").image  = image(cols*20, rows*20, 32)
  end if
  member("finalDecalImage").image  = image(cols*20, rows*20, 32)
  gDecalColors = []
  member("fogImage").image = image(cols*20,rows*20,32)
  
  member("dpImage").image = image(cols*20,rows*20,32)
  member("dpImage").image.copyPixels(DRPxl, pstRct, rect(0,0,1,1), {#color:255})
  member("rainBowMask").image = image(cols*20, rows*20, 1)
  member("flattenedGradientA").image = image(cols*20, rows*20, 16)
  member("flattenedGradientB").image = image(cols*20, rows*20, 16)
  if gLevel.lightType = "No Light" and not gDisableLights then
    member("shadowImage").image.copyPixels(DRPxl, member("shadowImage").image.rect, DRPxl.rect)
  end if
  gSmpl = image(4,1,32)
  gSmpl2 = image(30, 1, 32)
  gSmplPs = 0
  if not gDisableLights then
    inv = image(cols*20, rows*20, 1)
    inv.copyPixels(DRPxl, pstRct, rect(0,0,1,1), {#color:255})
    inv.copyPixels(member("shadowImage").image, pstRct, pstRct, {#ink:36, #color:color(255,255,255)})
    member("shadowImage").image.copyPixels(inv, pstRct, pstRct)
  end if
  member("dumpImage").image = image(cols*20,rows*20, 32)
end

on addShortcuts me, q2
    type inv: image
    type q2: number
    type pstrct: rect

    
    if q2 = 20 or q2 = 10 or q2=30 then
        pstRct = rect(point(0,0),point(gLOprops.size.loch*20,gLOprops.size.locv*20))
        inv = makeSilhoutteFromImg(member("finalImage").image, 1)
        repeat with xq = 1 to gLOprops.size.loch then
          repeat with xc = 1 to gLOprops.size.locv then
            
              if q2 = 20 and (gLEProps.matrix[xq][xc][1][2].getPos(5) > 0)and(gLEProps.matrix[xq][xc][1][1]=0)and(gLEProps.matrix[xq][xc][2][1]=1) then
                pasteShortCutHole("finalImage", point(xq,xc), 5, "BORDER")
                pasteShortCutHole("finalImage", point(xq,xc), 5, color(51,10,0))
              else if q2 = 10 and (gLEProps.matrix[xq][xc][1][2].getPos(5) > 0)and(gLEProps.matrix[xq][xc][1][1]=0)and(gLEProps.matrix[xq][xc][2][1]=0)and(gLEProps.matrix[xq][xc][3][1]=1) then
                pasteShortCutHole("finalImage", point(xq,xc), 15, "BORDER")
                pasteShortCutHole("finalImage", point(xq,xc), 15, color(41,9,0))
              else if q2=30 and (gLEProps.matrix[xq][xc][1][2].getPos(5) > 0)and(gLEProps.matrix[xq][xc][1][1]=1) then
                pasteShortCutHole("finalImage", point(xq,xc), -5, "BORDER")
                pasteShortCutHole("finalImage", point(xq,xc), -5, color(31,8,0))
              end if
          end repeat
        end repeat
        member("finalImage").image.copyPixels(inv, pstRct, pstRct, {#ink:36, #color:color(255,255,255)})
    end if
end

on finalizeLayer me, cValue
  type q: number
  type cValue: number
  type cols: number
  type rows: number
  type dp: number
  type pstrct: rect
  type inv: image
  type gSmpl: image
  type gSmpl2: image
  type gSmplPs: number
  type l: string
  type lr: number
  
  
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
  
  pstRct = rect(point(0,0),point(cols*20,rows*20))
  q = cValue
  --repeat with q = 1 to 30 then
  lr = 30-cValue
    dp = lr-5
    
    
    member("dpImage").image.copyPixels(member("layer"&string(lr)).image, pstRct, pstRct, {#ink:36, #color:color(255,255,255)})
    gSmpl.copyPixels(DRPxl, rect(gSmplPs,0,4,1), rect(0,0,1,1), {#color:0})
    
    if (lr=12)or(lr=8)or(lr=4)then
      gSmpl.copyPixels(DRPxl, rect(0,0,4,1), rect(0,0,1,1), {#blend:10, #color:255})
      gSmplPs = gSmplPs + 1
      member("dpImage").image.copyPixels(DRPxl, pstRct, rect(0,0,1,1), {#blend:10, #color:255})
    end if
    
    member("fogImage").image.copyPixels(member("layer"&string(lr)).image, pstRct, pstRct, {#ink:36, #color:color(255,255,255)})
    member("fogImage").image.copyPixels(DRPxl, pstRct, rect(0,0,1,1), {#blend:5, #color:255})


    gSmpl2.setPixel(cValue-1, 0, color(255, 255, 255))
    gSmpl2.copyPixels(DRPxl, rect(0,0,30,1), rect(0,0,1,1), {#blend:5, #color:255})
  
    member("finalImage").image.copyPixels(member("layer"&string(lr)).image, pstRct, pstRct, {#ink:36})
    if (lr = 10) or (lr = 20) or (lr=0) then
      addShortcuts(cValue)
    end if

    repeat with L in ["A", "B"] then
      
      member("dumpImage").image.copyPixels(member("gradient" & L & string(lr)).image, pstRct, pstRct)
      
      member("flattenedGradient" & L).image.copyPixels(member("dumpImage").image, pstRct, pstRct, {#maskImage:makeSilhoutteFromImg(member("layer" & string(lr)).image, 0).createMask()})
      member("flattenedGradient" & L).image.setPixel(0,0, color(0,0,0))
      member("flattenedGradient" & L).image.setPixel(cols*20-1,rows*20-1, color(0,0,0))
      
    end repeat
    if(gAnyDecals)then
      member("dumpImage").image.copyPixels(member("layer" & string(lr) & "dc").image, pstRct, pstRct)
      member("finalDecalImage").image.copyPixels(member("dumpImage").image, pstRct, pstRct, {#maskImage:makeSilhoutteFromImg(member("layer" & lr).image, 0).createMask()})
      member("finalDecalImage").image.setPixel(0,0, color(0,0,0))
      member("finalDecalImage").image.setPixel(cols*20-1,rows*20-1, color(0,0,0))
    end if

    DRFinalImage = member("finalImage").image
    DRFogImage = member("fogImage").image
    DRDpImage = member("dpImage").image
    DRShadowImage = member("shadowImage").image
    DRRainbowMask = member("rainBowMask").image
    DRFlattenedGradientA = member("flattenedGradientA").image
    DRFlattenedGradientB = member("flattenedGradientB").image
    DRFinalDecalImage = member("finalDecalImage").image

    dptsL = []
    
    fogDptsL = []
    -- todo: what are these?
    repeat with dpq = 1 to 4 then
      dptsL.add(gSmpl.getPixel(4-dpq, 0))
    end repeat
    
    repeat with dpq = 1 to 30 then
      fogDptsL.add(gSmpl2.getPixel(30-dpq, 0))
    end repeat

    -- mask this layer's final image
    inv = makeSilhoutteFromImg(member("layer" & string(lr)).image, 1)
    member("finalImage").image.copyPixels(inv, pstRct, pstRct, {#ink:36, #color:color(255,255,255)})


    
end

on paintDecalColors me
    repeat with dxq = 0 to gDecalColors.count - 1
      DRFinalImage.setPixel(dxq, 0, gDecalColors[dxq + 1])
    end repeat
end

on processColors me, pxc, pxq
  type pxc: number
  type pxq: number

  layer: number = 1
        
  getColor = DRFinalImage.getPixel(pxq, pxc)
  if (getColor <> color(255, 255, 255)) then
    if (getColor.green > 7) and (getColor.green < 11) then
      
    else if (getColor = color(0, 11, 0)) then
      -- put "What's this?"
      DRFinalImage.setPixel(pxq, pxc, color(10, 0, 0))
    else
      
      if (getColor = color(255, 255, 255)) then
        layer = 0
      end if
      lowResDepth = dptsL.getPos(DRDpImage.getPixel(pxq, pxc))
      fgDp = fogDptsL.getPos(DRFogImage.getPixel(pxq, pxc))
      fogFac = (255-DRFogImage.getPixel(pxq, pxc).red)/255.0
      fogFac = (fogFac - 0.0275)*(1.0/0.9411)
      rainBowFac = 0
      
      
      -- if member("blackOutImg2").image.getPixel(pxq, pxc) = 0 then
      if fogFac <= 0.2 then
        repeat with dsplc in [point(-2,0), point(0,-2), point(2, 0), point(0,2), point(-1,-1), point(1,-1), point(1, 1), point(-1,1)] then
          otherFogFac = (255-DRFogImage.getPixel(restrict(pxq+dsplc.locH, 0, gLOprops.size.loch * 20 -1), restrict(pxc+dsplc.locV, 0, gLOprops.size.locv * 20 - 1)).red)/255.0
          otherFogFac = (otherFogFac - 0.0275)*(1.0/0.9411)
          rainBowFac = rainBowFac + (abs(fogFac-otherFogFac)>0.0333)*(restrict(fogFac - otherFogFac, 0, 1)+1)
          if rainBowFac > 5 then
            exit repeat
          end if
        end repeat
        
        rainBowFac = (rainBowFac>5)
      end if
      -- end if
      
      col = color(0, 0, 0)
      
      transp = false
      
      palCol = 2
      effectColor = 0
      dark = 0
      
      case string(getColor) of
        "color( 255, 0, 0 )":
          palCol = 1
        "color( 0, 255, 0 )":
          palCol = 2
        "color( 0, 0, 255 )":
          palCol = 3
        "color( 255, 0, 255 )":
          palCol = 2
          effectColor = 1
        "color( 0, 255, 255 )":
          palCol = 2
          effectColor = 2
        "color( 255, 150, 255 )":
          palCol = 3
          effectColor = 1
        "color( 150, 255, 255 )":
          palCol = 3
          effectColor = 2
        "color( 150, 0, 0 )":
          palCol = 1
          dark = 1
        "color( 0, 150, 0 )":
          palCol = 2
          dark = 1
        "color( 0, 0, 150 )":
          palCol = 3
          dark = 1
        "color( 150, 0, 150 )":
          palCol = 1
          effectColor = 1
          --dark = 1
        "color( 0, 150, 150 )":
          palCol = 1
          effectColor = 2
          --dark = 1
      end case
      
      if(getColor.green = 255)and(getColor.blue = 150)then
        palCol = 1
        effectColor = 3
      end if
      
      
      --if transp = false then
      col.red = ((palCol-1) * 30) + fgDp
      
      if gDisableLights = 0 and (DRShadowImage.getPixel(pxq, pxc) <> color( 0, 0, 0 )) then
        col.red = col.red + 90
      end if
      
      greenCol = effectColor
      
      
      if (grimeActive) then
        if (rainBowFac) then
          if (grimeOnGradients) then
            greenCol = greenCol + 4
            me.rainbowifypixel(point(pxq+1,pxc+1))
          else if (greenCol <> 1) and (greenCol <> 2) and (greenCol <> 3) then
            greenCol = greenCol + 4
            me.rainbowifypixel(point(pxq+1,pxc+1))
          end if
        else if (DRRainBowMask.getPixel(pxq, pxc) <> color( 0 )) then
          if (grimeOnGradients) then
            greenCol = greenCol + 4
          else if (greenCol <> 1) and (greenCol <> 2) and (greenCol <> 3) then
            greenCol = greenCol + 4
          end if
        end if
      end if
      -- put member("rainBowMask").image.getPixel(pxq, pxc)
      
      
      if (effectColor > 0) then
        if (effectColor = 3) then
          col.blue = getColor.red
        else
          modABEf = [TRUE, FALSE][(effectColor mod 4)]
          if (modABEf) then
            col.blue = 255-DRFlattenedGradientA.getPixel(pxq, pxc).red
          else
            col.blue = 255-DRFlattenedGradientB.getPixel(pxq, pxc).red
          end if
        end if
        if (col.blue >= 255) and (bkgFix) then
          col.blue = 254
        end if
      else
        decalColor = 0
        if(gAnyDecals)then
          dcGet = DRFinalDecalImage.getPixel(pxq, pxc)
          if (dcGet <> color(255, 255, 255))then
            if(dcGet = gPEcolors[1][2])then
              if (grimeActive) then
                if (grimeOnGradients) then
                  if(doesGreenValueMeanRainbow(greenCol) = 0)then--RAINBOW DECAL COLOR!
                    greenCol = greenCol + 4
                  end if
                else if (greenCol <> 1) and (greenCol <> 2) and (greenCol <> 3) then
                  if(doesGreenValueMeanRainbow(greenCol) = 0)then--RAINBOW DECAL COLOR!
                    greenCol = greenCol + 4
                  end if
                end if
              end if
            else
              decalColor = gDecalColors.getPos(dcGet)
              if(decalColor=0)and(gDecalColors.count < 255)then
                gDecalColors.add(dcGet)
                decalColor = gDecalColors.count
              end if
              if (bkgFix) and (decalColor < 2) then
                decalColor = 2
              end if
              col.blue = 256-decalColor
              greenCol = greenCol + 8
            end if
          end if
        end if
        
      end if
      
      col.green = greenCol + (dark*16)
      
      if layer = 0 then
        DRFinalImage.setPixel(pxq, pxc, color(255, 255, 255))
      else
        DRFinalImage.setPixel(pxq, pxc, col)
      end if
      
    end if
    --end if
  end if
end

on rainbowifypixel me, pxl
  if(pxl.locH < 2)or(pxl.locV < 2)then
    return
  end if
  
  if IsPixelInFinalImageRainbowed(pxl+point(-1, 0)) = 0 then
    currCol = DRFinalImage.getPixel(pxl.locH-1-1, pxl.locV-1)
    DRFinalImage.setPixel(pxl.locH-1-1, pxl.locV-1, color(currCol.red, currCol.green+4, currCol.blue))
  end if
  
  if IsPixelInFinalImageRainbowed(pxl+point(0, -1)) = 0 then
    currCol = DRFinalImage.getPixel(pxl.locH-1, pxl.locV-1-1)
    DRFinalImage.setPixel(pxl.locH-1, pxl.locV-1-1, color(currCol.red, currCol.green+4, currCol.blue))
  end if
  
  --if (pxl.locH >= 0) and (pxl.locV-1 >= 0) and (pxl.locH <= 1400) and (pxl.locV-1 <= 800) then
  DRRainBowMask.setPixel(pxl.locH-1+1, pxl.locV-1, color(0, 0, 0))
  --end if
  --if (pxl.locH-1 >= 0) and (pxl.locV >= 0) and (pxl.locH-1 <= 1400) and (pxl.locV <= 800) then
  DRRainBowMask.setPixel(pxl.locH-1, pxl.locV-1+1, color(0, 0, 0))
  --end if
end

on IsPixelInFinalImageRainbowed(pxl)
  if(pxl.loch < 1)or(pxl.locv < 1)then
    return 0
  else if(DRFinalImage.getPixel(pxl.locH-1, pxl.locV-1) = color(255, 255, 255))then
    return 0
  else
    grn = DRFinalImage.getPixel(pxl.locH-1, pxl.locV-1).green
    return doesGreenValueMeanRainbow(grn)
  end if
  
end

on doesGreenValueMeanRainbow(grn)
  type return: number
  if (grn > 3)and(grn < 8)then
    return 1
  else  if (grn > 11)and(grn < 16)then
    return 1
  else 
    return 0
  end if
end