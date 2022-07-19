# Kobayashi Estimator

## Getting Started

Install [Lerna](https://lerna.js.org/) globally:

```bash
yarn global add lerna
```

## Installing Packages

```bash
lerna bootstrap
```

############

## Set up the package and binaries

1. Install the CGM Package and the Zea package
2. Unzip the kobayashi-estimator.zip file to a location of your choice. This is the source code for the web app.
3. Unzip CGMBendRecog.zip located in the CGMApp directory of the web app in CGM InstallDir\samples\cs
4. Update spatial_license.h and spatial_license.cs
5. Update LAUNCH_CGMBendRecogSLN.bat and Build CGMBendRecog 
6. Launch Launch_SPAZea_64.bat and build SPAToZCadBridge

You should have CGM InstallDir\win_b64\code\clr\CGMBendRecog.exe and CGMInstallDir\win_b64\code\bin\SPAToZCadBridge.exe

## Update SetEnv.bat file to point to the appropriate paths

SetEnv.bat file is located in the root directory of the web app.
Edit this file to update the `PATH` variable.

```bash
set PATH=E:\cgm-install-folder\win_b64\code\clr;E:\cgm-install-folder\win_b64\code\bin;%PATH%
```
NB: This won't work if you use vscode and powershell
    $env:PATH = 'E:\WebCS\CGM_2022.1.0.1\win_b64\code\clr;E:\WebCS\CGM_2022.1.0.1\win_b64\code\bin;' + $env:PATH
    dir env:PATH

## Set up and run the backend server
Launch a command prompt in the root folder of the web app
Then run the following: 
```bash
SetEnv.bat
cd packages\backend
yarn
yarn run dev
```

## Set up and run the frontend server
Launch a command prompt in the root folder of the web app
Then run the following: 
```bash
cd packages\frontend
yarn
yarn run dev
```

## Launch and run the web app
1. Launch a browser (e.g. MS Edge, Firefox, Chrome)
2. Go to http://localhost:5000/
3. Drag and drop a CAD file into the window
4. Once you see the model rendered on the screen, you can click the "Estimate" button
5. Once the model has been processed the text on the rightside panel with display the number of bends and the thickness, and a cost estimate (using a simple dummy formula).

## Additional Source Code
The source code for `CGMBendRecog.exe` is provided via a zip file in the DS File Transfer workspace.

Edit the `LAUNCH_CGMBendRecogSLN.bat` file to point the `CGMPATH` variable to the location of the CGM package on your machine.

Then run `LAUNCH_CGMBendRecogSLN.bat` to launch the Visual Studio solution.

## Starting Dev Server

```bash
lerna run dev
```
