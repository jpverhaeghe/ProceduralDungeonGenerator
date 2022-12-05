using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class DungeonGeneration : MonoBehaviour
{
    // Enumerated types
    // All possible combinations of rooms with exits that could be placed...
    // (2^4 - 1) as we don't have a room with no exits
    private enum RoomType
    {
        CLR,
        NESW,
        NES,
        NEW,
        NSW,
        ESW,
        NE,
        NS,
        NW,
        ES,
        EW,
        SW,
        N,
        E,
        S,
        W
    }

    private enum ExitDirection
    {
        N,
        E,
        S,
        W
    }

    // Constant values
    private const float PERCENT_DIVISOR = 100.0f;

    private static RoomType[] roomTypes = 
    {   // This will help in randomization of the rooms as we need to be able to access the values
        RoomType.NESW,
        RoomType.NES,
        RoomType.NEW,
        RoomType.NSW,
        RoomType.ESW,
        RoomType.NE,
        RoomType.NS,
        RoomType.NW,
        RoomType.ES,
        RoomType.EW,
        RoomType.SW,
        RoomType.N,
        RoomType.E,
        RoomType.S,
        RoomType.W 
    };


    // Serialized Fields for use in Unity
    [Header("Dungeon Size Height")]
    [SerializeField] Slider dungeonHeightSlider;
    [SerializeField] TextMeshProUGUI dungeonHeightText;

    [Header("Dungeon Size Width")]
    [SerializeField] Slider dungeonWidthSlider;
    [SerializeField] TextMeshProUGUI dungeonWidthText;

    [Header("Max Percent Clear Rooms")]
    [SerializeField] Slider dungeonClearSlider;
    [SerializeField] TextMeshProUGUI dungeonClearText;

    [Header("UI Output")]
    [SerializeField] TextMeshProUGUI startingLocation;  // a text box to print the array the starting room information each generation
    [SerializeField] TextMeshProUGUI generationOutbox;  // a text box to print the array in after each generation

    // Public variables

    // Private variables
    private RoomType[,] generatedDungeon;               // an array to hold the room types for the dungeon being generated
    private int startRoomRow;                           // the starting room Z position for the dungeon
    private int startRoomCol;                           // the starting room X position for the dungeon
    private int dungeonRowSize;                         // the dungeon length given by user (limiting from 10 to 100)
    private int dungeonColSize;                         // the dungeon width given by user (limiting from 10 to 100)
    private int currentRoomHeight;                      // the current size Z of the dungeon for printing
    private int currentRoomWidth;                       // the current size X of the dungeon for printing
    private int emptyRoomPct;                           // the percentage of empty rooms the user wants (limiting to 25% to 75%)
    private int minNumEmpty;                            // a minimum number of empty rooms based on dungeon size and user input
    private int dungeonNumber = 0;                      // a dungeon number to keep track of files to create (save to playerPrefs?)


    // Start is called before the first frame update
    void Start()
    {

        // set up the high score from last time the scene was loaded
        if (PlayerPrefs.HasKey("DungeonNumber"))
        {
            dungeonNumber = PlayerPrefs.GetInt("DungeonNumber");
        }

        // set up initial slider values for the dungeon generation system (height)
        dungeonRowSize = (int)dungeonHeightSlider.value;
        dungeonHeightText.text = dungeonRowSize.ToString();

        // (width)
        dungeonColSize = (int)dungeonWidthSlider.value;
        dungeonWidthText.text = dungeonColSize.ToString();

        // (max clear room percentage)
        emptyRoomPct = (int)dungeonClearSlider.value;
        dungeonClearText.text = emptyRoomPct.ToString();

        // Generate the dungeon for this this play through
        GenerateDungeon();

    } // end Start

    // Update is called once per frame - may not be needed in this instance
    void Update()
    {
        
    } // end Update

    /// <summary>
    /// Clears the dungeon room type array
    /// </summary>
    public void ClearDungeon()
    {
        // Set up the room dungeon to be empty by populating it with the CLR Room type
        for (int i = 0; i < currentRoomHeight; i++)
        {
            for (int j = 0; j < currentRoomWidth; j++)
            {
                generatedDungeon[i, j] = RoomType.CLR;
            }
        }

    } // end ClearDungeon

    /// <summary>
    /// Generates the dungeon room system using room types in the dungeon array to be replaced later with rooms
    /// </summary>
    public void GenerateDungeon()
    {
        // save the current size of the dungeon being generated so we don't go out of bounds when printing clearing if it changes
        currentRoomHeight = dungeonRowSize;
        currentRoomWidth = dungeonColSize;

        // Create the room dungeon array based on the user input, this won't change during game play
        generatedDungeon = new RoomType[currentRoomHeight, currentRoomWidth];

        // calculate the largest number of empty rooms for re-generation
        float emptyPercent = (emptyRoomPct / PERCENT_DIVISOR);
        int numRooms = (currentRoomHeight * currentRoomWidth);
        minNumEmpty = (int)(numRooms * emptyPercent);

        // Clear the dungeon to get it ready to be populated
        ClearDungeon();

        // Choose a random location for the starting room. 
        //      - to make sure this works, we need to make sure our starting room is not on the edge of the array 
        //      - unless you want pass through
        startRoomRow = Random.Range(1, (currentRoomHeight - 1));
        startRoomCol = Random.Range(1, (currentRoomWidth - 1));

        // set the starting data to the background so we can trace the output 
        startingLocation.text = "Starting row is " + (startRoomRow + 1) + " and col is " + (startRoomCol + 1);

        // TODO: Randomize a room for that point
        // For now, all starting rooms will have all four exits (NESW)
        generatedDungeon[startRoomRow, startRoomCol] = RoomType.NESW;

        // Now that we have a starting room, go through and create rooms in adjoining areas and
        // go through the exits of that room starting with N as we have to have a starting point
        // (use recursion to go through entire array to verify what room can be placed in what location)
        // TODO: Later have rooms that start with different exits and only check those exits only
        GenerateRoom(ExitDirection.S, startRoomRow - 1, startRoomCol);       // North Exit (so send south exit down)

        // Print the current array after the room is printed
        //DebugPrintDungeonArray();

        GenerateRoom(ExitDirection.W, startRoomRow, startRoomCol + 1);       // East Exit (so send west exit down)

        // Print the current array after the room is printed
        //DebugPrintDungeonArray();

        GenerateRoom(ExitDirection.N, startRoomRow + 1, startRoomCol);       // South Exit (so send north exit down)

        // Print the current array after the room is printed
        //DebugPrintDungeonArray();

        GenerateRoom(ExitDirection.E, startRoomRow, startRoomCol - 1);       // West Exit (so send east exit down)

        // Print the current array after the room is printed
        int numClrRooms = DebugPrintDungeonArray();

        // printing some debug information to test percentages
        Debug.Log("Dungeon generated with " + numClrRooms + " clear rooms when tolerance was: " + minNumEmpty);

        // check number of empty rooms and if it is too many - regenerate!
        // This will keep dungeons from being too small based on user tolerance
        if (numClrRooms > minNumEmpty)
        {
            Debug.Log("Regenerating");
            GenerateDungeon();
        }

    } // end GenerateDungeon

    /// <summary>
    /// This function is designed to be set to a print button and it saves the dungeon data to .csv fileName to be used at a later date
    /// This will delete a fileName if it already exists - replacing it for now.
    /// This stackOverflow was helpful in figuring out how to write to a fileName:
    /// https://stackoverflow.com/questions/54681817/saving-streamed-data-from-unity-in-a-csv-fileName
    /// </summary>
    public void SaveDungeon()
    {
        string currentDir = Directory.GetCurrentDirectory();
        string filePath = @".\GeneratedDungeons\";
        string fileName = filePath + "Dungeon_" + dungeonNumber + ".csv";
        string delimiter = ", ";

        // check to see if the directory exists, and if not, create it
        if (!Directory.Exists(filePath) )
        {
            Directory.CreateDirectory(filePath);
        }

        // This will delete a fileName if it already exists - replacing it for now.
        if (File.Exists(fileName) )
        {
            File.Delete(fileName);
        }

        // now get the string to ouput (let's add the start row and column at the beginning)
        string outputString = "";

        for (int row = 0; row < currentRoomHeight; row++)
        {
            for (int col = 0; col < currentRoomWidth; col++)
            {
                // add each room to the print out so we see it as a double array using the delimiter
                outputString += generatedDungeon[row, col].ToString() + delimiter;
            }

            // a new line for each row
            outputString += "\n";
        }

        // before writing to the file, store the starting information
        outputString += startingLocation.text;

        // write to the fileName
        File.WriteAllText(fileName, outputString);

        // increase the dungeon number so we can save more files 
        dungeonNumber++;

        // saving the current value so we don't overwrite files later
        PlayerPrefs.SetInt("DungeonNumber", dungeonNumber);

    } // end SaveDungeon

    /// <summary>
    /// Updates the width of the dungeon for next generation.
    /// </summary>
    public void updateDungeonHeight() 
    {
        dungeonRowSize = (int)dungeonHeightSlider.value;
        dungeonHeightText.text = dungeonRowSize.ToString();

    } // updateDungeonHeight

    /// <summary>
    /// Updates the width of the dungeon for next generation.
    /// </summary>
    public void updateDungeonWidth()
    {
        dungeonColSize = (int)dungeonWidthSlider.value;
        dungeonWidthText.text = dungeonColSize.ToString();

    } // updateDungeonWidth

    /// <summary>
    /// Updates the clear room percentage of the dungeon for next generation.
    /// </summary>
    public void updateDungeonEmptyPercent()
    {
        emptyRoomPct = (int)dungeonClearSlider.value;
        dungeonClearText.text = emptyRoomPct.ToString();

    } // updateDungeonEmptyPercent

    /// <summary>
    /// to exit the program on a windows build
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();

    } // end QuitGame

    /// <summary>
    /// Generates a room to place in the current spot and goes through exits until it can't generate any more rooms (recursive)
    /// </summary>
    /// <param name="exitNeeded">The exit that must exist in the new room</param>
    /// <param name="currentRow">the row position in the array to place the new room</param>
    /// <param name="currentCol">the column position in the array to place the new room</param>
    private void GenerateRoom(ExitDirection exitNeeded, int currentRow, int currentCol)
    {
        // Grab the roomType and 
        RoomType currentRoom = generatedDungeon[currentRow, currentCol];

        // if this room has not been populated already, then find a room to put in this place
        // otherwise we are at a (Base Case) and can just exit
        if (currentRoom == RoomType.CLR)
        {
            // generate a room that conains the correct exit direction using string values of the enum
            while (!currentRoom.ToString().Contains(exitNeeded.ToString() ) )
            {
                // randomizing an enum is a little more difficult as we don't have an array - so created a room array to hold the values
                currentRoom = roomTypes[Random.Range(0, roomTypes.Length)];

                // if an exit is going out of bounds, mark the room as clear and try again
                if ( (currentRoom.ToString().Contains(ExitDirection.N.ToString() ) && (currentRow == 0) ) ||
                     (currentRoom.ToString().Contains(ExitDirection.E.ToString() ) && (currentCol == (currentRoomWidth - 1) ) ) ||
                     (currentRoom.ToString().Contains(ExitDirection.S.ToString() ) && (currentRow == (currentRoomHeight - 1) ) ) || 
                     (currentRoom.ToString().Contains(ExitDirection.W.ToString() ) && (currentCol == 0 ) ) )
                {
                    currentRoom = RoomType.CLR;
                }

                // Check surrounding rooms if we don't have a clear room
                if (currentRoom != RoomType.CLR)
                {
                    currentRoom = CheckSurroundingRooms(currentRoom, currentRow, currentCol);
                }
            }

            generatedDungeon[currentRow, currentCol] = currentRoom;

            // check each exit direction and generate the room
            // start with North (row)
            if (currentRoom.ToString().Contains(ExitDirection.N.ToString() ) )
            {
                GenerateRoom(ExitDirection.S, currentRow - 1, currentCol);
            }

            // then East (col)
            if (currentRoom.ToString().Contains(ExitDirection.E.ToString()))
            {
                GenerateRoom(ExitDirection.W, currentRow, currentCol + 1);
            }

            // then South (row)
            if (currentRoom.ToString().Contains(ExitDirection.S.ToString()))
            {
                GenerateRoom(ExitDirection.N, currentRow + 1, currentCol);
            }

            // then West (col)
            if (currentRoom.ToString().Contains(ExitDirection.W.ToString()))
            {
                GenerateRoom(ExitDirection.E, currentRow, currentCol - 1);
            }
        }

    } // end GenerateRoom
    
    /// <summary>
    /// Checks surrounding rooms to verify if an exit is in the direction if needed, returns a clear room if not
    /// </summary>
    /// <param name="currentRoom">The current room type to check</param>
    /// <param name="currentRow">The current row in the generation array</param>
    /// <param name="currentCol">The current col in the generation array</param>
    /// <returns></returns>
    private RoomType CheckSurroundingRooms(RoomType currentRoom, int currentRow, int currentCol)
    {

        // only look North if we aren't on the top edge
        if (currentRow > 0)
        {
            RoomType roomNorth = generatedDungeon[currentRow - 1, currentCol];

            // only need to check if there is a room there.
            if (roomNorth != RoomType.CLR)
            {

                // if the room contains a South exit, then also check that we have a North exit
                if (roomNorth.ToString().Contains(ExitDirection.S.ToString()))
                {
                    // if the room does not contain a North exit, then try again
                    if (!currentRoom.ToString().Contains(ExitDirection.N.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
                // otherwise it must have a room that doesn't have a South exit
                else
                {
                    // so make sure we don't have one North
                    if (currentRoom.ToString().Contains(ExitDirection.N.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
            }
        }

        // only look South if we aren't on the bottom edge
        if (currentRow < (currentRoomHeight - 1))
        {
            RoomType roomSouth = generatedDungeon[currentRow + 1, currentCol];

            // only need to check if there is a room there.
            if (roomSouth != RoomType.CLR)
            {
                // if the room contains a North exit, then also check that we have a South exit 
                if (roomSouth.ToString().Contains(ExitDirection.N.ToString()))
                {
                    // if the room does not contain a South exit, then try again
                    if (!currentRoom.ToString().Contains(ExitDirection.S.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
                // otherwise it must have a room that doesn't have a North exit
                else
                {
                    // so make sure we don't have one South
                    if (currentRoom.ToString().Contains(ExitDirection.S.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
            }
        }

        // only look West if we aren't on the left edge
        if (currentCol > 0)
        {
            RoomType roomWest = generatedDungeon[currentRow, currentCol - 1];

            // only need to check West if there is a room there
            if (roomWest != RoomType.CLR)
            {
                // if the room contains an East exit, then also check to see if we have a West exit
                if (roomWest.ToString().Contains(ExitDirection.E.ToString()))
                {
                    // if the room does not contains a West exit, then try again
                    if (!currentRoom.ToString().Contains(ExitDirection.W.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
                // otherwise it must have a room that doesn't have an East exit
                else
                {
                    // so make sure we don't have one West
                    if (currentRoom.ToString().Contains(ExitDirection.W.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
            }
        }

        // only look East if we aren't on the right edge
        if (currentCol < (currentRoomWidth - 1))
        {
            RoomType roomEast = generatedDungeon[currentRow, currentCol + 1];

            // only need to check East if there is a room there
            if (roomEast != RoomType.CLR)
            {
                // if the room contains a West exit, then also check to see if we have an East exit
                if (roomEast.ToString().Contains(ExitDirection.W.ToString()))
                {
                    // if the room does not contains an East exit, then try again
                    if (!currentRoom.ToString().Contains(ExitDirection.E.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
                // otherwise it must have a room that doesn't have a West exit
                else
                {
                    // so make sure we don't have one East
                    if (currentRoom.ToString().Contains(ExitDirection.E.ToString()))
                    {
                        currentRoom = RoomType.CLR;
                    }
                }
            }
        }

        return currentRoom;

    } // end CheckSurroundingRooms

    private int DebugPrintDungeonArray()
    {
        int numClrRooms = 0;

        string outputString = "";

        for (int row = 0; row < currentRoomHeight; row++)
        {
            for (int col = 0; col < currentRoomWidth; col++)
            {
                // keep track of empty rooms, as we may need to regenerate the dungeon if there are too many
                if (generatedDungeon[row, col] == RoomType.CLR)
                {
                    numClrRooms++;
                }

                // add each room to the print out so we see it as a double array
                outputString += generatedDungeon[row, col].ToString() + " ";

                // add spaces to make it even per column - biggest Room type is NESW, rest are less
                for (int spaces = 0; spaces < (RoomType.NESW.ToString().Length - generatedDungeon[row, col].ToString().Length); spaces++)
                {
                    outputString += " ";
                }
            }

            // a new line for each row
            outputString += "\n";
        }

        generationOutbox.text = outputString;
        //Debug.Log(outputString);

        return numClrRooms;

    } // end DebugPrintDungeonArray

}
