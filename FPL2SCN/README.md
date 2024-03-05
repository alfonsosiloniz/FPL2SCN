# FPL2SCN

## Overview
This tool is able to generate a MSFS2020 Scenery using 
* a MSFS Flight plan file (PLN format) to help you to reference visually in a VFR flight for example.
* a KML (v2.2) file (From Google Earth for instance) with Placemarks

You don´t need to use the MSFS SDK to obtain the Package. This Package can be copied and pasted directly in your Community folder.

## Features

* It detects automatically the type of XML file is going to process among FPL or KML file formats
* with the -x option it can generate the XML file for PackageSources if you want to use it with the MSFS SDK
- You can drag&drop a file on FPL2SCN.exe in Windows Explorer for the application to run
- You can also run it in the command line or PowerShell

## How to use

You need to generate a KML file (With Google Earth or others) or a PLN flight plan for MSFS2020 (With Little Nav Map or Plan G, etc) as usual. 

If you want that the tool includes any of those coordinate points in the MSFS scenery, you need to prefix them in the name with any of the KEY values included in the fpl2scn.config file. 

For instance

    TOWER Tower on Cadiz
    YATCH A ship on the bay in PONT LEMG-S

The tool uses those keys to identify that the point is to be processed, and to look what is the MSFS object that will be located at that specific coordinates. A sample file is included with many Library objects that are present in the Asobo Default libraries, so you can use it right away. 

But it is also possible to define other Keys and GUID for objects in other proprietary libraries. The only that you need is to know the GUID of each of the objects to be used.

Any point in the files that don´t include a KEY will be ignored when creating the scenery.

Once finished, you can directly copy the contents on MSFS_Package into your Community folder in MSFS 2020.


## Execution

### Drag & Drop

Just click on the PLN or KML file you want to use to generate the scenery and drag and drop it on the executable file FPL2SCN.exe 

A console window will open where you can see the result of the operation. Press any key to finish when done.

### Command Line options

    filename    Required. Input file path + name (KML or PLN)

    -o, --output    Output file path for XML file. By default it will use the included SeceneryProject folder under PackageSources

    -x, --xml       Generate the XML file.

    --help          Display this help screen.

    --version       Display version information.


## Configuration

The tool uses the file fpl2scn.config file to configure a list of Keys and Values that relate a prefix CODE and the GUID of the library to use

    {
        "PointsDefinition": {
            "RMTOWER": "7A8E0FFF-3B2B-44EA-6162-4DF99C6D3585",
            "ANTENNA": "59EDCF34-E034-4DC0-85B0-B7D7319E8DD3",
            "BEACON": "A04B772A-6BBF-4103-B263-D479BCCE71F5"
    }


Just remember to use the same KEYs that are defined in the file to prefix to the name pf your points in the PLN or KML file.

