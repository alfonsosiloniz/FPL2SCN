# BGLLibrary

## Overview

BGLLibrary is a C# library that is able to generate a BGL file including multiple library objects located in the a MSFS Scenery.

The library is able to compile the binary BGL file **without** using the MSFS SDK, and just stand-alone.


## Features
- Implements a Point struct including the coordinates and GUID of the object that will be represented in the simulator.
- The library is able to compile the BGL file in binary (without compression)
- It computes the Header QMIDs implementing the algorithm described at https://www.fsdeveloper.com/wiki/index.php/BGL_File_Format even if it has more than 8 QMIDs
- It regenerate the layout.json after the BGL file is generated with the actual size of the BGL file
- It generates the BGL file in a MSFS Project folder that can be copied and pasted directly in the Community directory
- It is able of generating the Source XML file to be used with the MSFS SDK just in case
- It takes in account the altitude and heading defined in the points along with the coordinates

## License
This project is licensed under the MIT License - see the LICENSE file for details.