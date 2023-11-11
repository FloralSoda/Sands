@ECHO OFF
cls

if not exist "./assets" mkdir ./assets
if not exist "./assets/audio" mkdir ./assets/audio
if not exist "./assets/textures" mkdir ./assets/textures
if not exist "./assets/models" mkdir ./assets/models

echo 1. Pack
echo 2. Unpack

CHOICE /C 12 /M "Enter number next to function: "
set op=%errorlevel%

echo 1. Audio
echo 2. Models
echo 3. Textures
echo 4. All
echo 5. Models And Textures

CHOICE /C 12345 /M "Enter number next to media type: "
set media=%errorlevel%

IF %op%==2 GOTO Unpack
IF %op%==1 GOTO Pack

:Unpack

CHOICE /C YNA /M "Should these files overwrite the current files (A for Ask): "
set c=%errorlevel%

if %c%=="Y" set ow="-o+"
if %c%=="N" set ow="-o-"
if %c%=="A" set ow="-o"

If %media%==5 GOTO UModels
IF %media%==4 GOTO UAudio
IF %media%==3 GOTO UTextures
IF %media%==2 GOTO UModels
IF %media%==1 GOTO UAudio

:UAudio
echo Unpacking Audio
echo --------------------------
call unrar x %ow% ./audio.assets ./assets/audio
echo --------------------------
:: This only skips the other options if specifically Audio was picked. Other options will roll over
IF %media%==1 GOTO End
:UModels
echo Unpacking Models
echo --------------------------
call unrar x %ow% ./models.assets ./assets/models
echo --------------------------
:: This only skips the other options if specifically Models was picked. Other options will roll over
IF %media%==2 GOTO End
:UTextures
echo Unpacking Textures
echo --------------------------
call unrar x %ow% ./textures.assets ./assets/textures
echo --------------------------
:: No option to rollover from, so goes to end regardless of option
GOTO End

:Pack

IF %media%==5 GOTO PModels
IF %media%==4 GOTO PAudio
IF %media%==3 GOTO PTextures
IF %media%==2 GOTO PModels
IF %media%==1 GOTO PAudio

:PAudio
echo Packing Audio
echo --------------------------
cd ./assets/audio
call rar a -r ../../audio.assets .
echo --------------------------
cd ../..
:: This only skips the other options if specifically Audio was picked. Other options will roll over
IF %media%==1 GOTO End
:PModels
echo Packing Models
echo --------------------------
cd ./assets/models
call rar a -r ../../models.assets .
echo --------------------------
cd ../..
:: This only skips the other options if specifically Models was picked. Other options will roll over
IF %media%==2 GOTO End
:PTextures
echo Packing Textures
echo --------------------------
cd ./assets/textures
call rar a -r ../../textures.assets .
echo --------------------------
cd ../..
GOTO End

:End
ECHO Success.
Pause