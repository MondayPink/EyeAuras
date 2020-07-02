@echo off
cd Sources
mklink /J /D PoeShared "../Submodules/PoeEye/PoeEye/PoeShared"
mklink /J /D PoeShared.Wpf "../Submodules/PoeEye/PoeEye/PoeShared.Wpf"
mklink /J /D PoeShared.Native "../Submodules/PoeEye/PoeEye/PoeShared.Native"
mklink /J /D PoeShared.Tests "../Submodules/PoeEye/PoeEye/PoeShared.Tests"
mklink /J /D PoeShared.Squirrel "../Submodules/PoeEye/PoeEye/PoeShared.Squirrel"

echo Initializing Optional modules
mklink /J /D PoeShared.OpenCV "../Submodules/EyeAuras.Plus/PoeShared.OpenCV"
mklink /J /D EyeAuras.OpenCVAuras "../Submodules/EyeAuras.Plus/EyeAuras.OpenCVAuras"
mklink /J /D EyeAuras.AdvancedAuras "../Submodules/EyeAuras.Plus/EyeAuras.AdvancedAuras"
mklink /J /D EyeAuras.Loader "../Submodules/EyeAuras.Plus/EyeAuras.Loader"
mklink /J /D EyeAuras.Loader.Shared "../Submodules/EyeAuras.Plus/EyeAuras.Loader.Shared"
mklink /J /D EyeAuras.Web "../Submodules/EyeAuras.Plus/EyeAuras.Web"
mklink /J /D EyeAuras.Roxy "../Submodules/EyeAuras.Plus/EyeAuras.Roxy"
mklink /J /D EyeAuras.Roxy.U2K "../Submodules/EyeAuras.Plus/EyeAuras.Roxy.U2K"
mklink /J /D EyeAuras.Roxy.Kernel "../Submodules/EyeAuras.Plus/EyeAuras.Roxy.Kernel"
mklink /J /D EyeAuras.Roxy.Shared "../Submodules/EyeAuras.Plus/EyeAuras.Roxy.Shared"
mklink /J /D EyeAuras.Plus.Tests "../Submodules/EyeAuras.Plus/EyeAuras.Plus.Tests"

