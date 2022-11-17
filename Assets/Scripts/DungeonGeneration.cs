using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

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
    private const int TOTAL_ROOM_DIVISOR = 4;

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
    [Header("Dungeon Size")]
    [Range(10, 100)]
    [SerializeField] int dungeonRowSize = 10;           // the dungeon length given by user (defaults to 10)

    [Range(10, 100)]
    [SerializeField] int dungeonColSize = 10;           // the dungeon width given by user (defaults to 10)

    [Header("UI Output")]
    [SerializeField] TextMeshProUGUI startingLocation;  // a text box to print the array in after each pass (may need to have some wait time)
    [SerializeField] TextMeshProUGUI generationOutbox;  // a text box to print the array in after each pass (may need to have some wait time)

    // Public variables

    // Private variables
    private RoomType[,] generatedDungeon;               // an array to hold the room types for the dungeon being generated
    private int startRoomRow;                           // the starting room X position for the dungeon
    private int startRoomCol;                           // the starting room Z position for the dungeon
    private int currentRowSize;                         // the current size X of the dungeon for printing
    private int currentColSize;                         // the current size Z of the dungeon for printing
    private int minNumEmpty;                            // a minimum number of empty rooms based on dungeon size
    private int dungeonNumber = 0;                      // a dungeon number to keep track of files to create (save to playerPrefs?)


    // Start is called before the first frame update
    void Start()
    {
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
        for (int i = 0; i < currentRowSize; i++)
        {
            for (int j = 0; j < currentColSize; j++)
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
        currentRowSize = dungeonRowSize;
        currentColSize = dungeonColSize;

        // Create the room dungeon array based on the user input, this won't change during game play
        generatedDungeon = new RoomType[currentRowSize, currentColSize];

        // calculate the largest number of empty rooms for re-generation
        minNumEmpty = ((currentRowSize * currentColSize) / TOTAL_ROOM_DIVISOR);

        // Clear the dungeon to get it ready to be populated
        ClearDungeon();

        // Choose a random location for the starting room. 
        //      - to make sure this works, we need to make sure our starting room is not on the edge of the array 
        //      - unless you want pass through
        startRoomRow = Random.Range(1, (currentRowSize - 1));
        startRoomCol = Random.Range(1, (currentColSize - 1));

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

        // TODO: may need to check number of empty rooms and if it is too many - regenerate! This will keep dungeons from being too small
        if (numClrRooms > minNumEmpty)
        {
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

        for (int row = 0; row < currentRowSize; row++)
        {
            for (int col = 0; col < currentColSize; col++)
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

    } // end SaveDungeon

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
                     (currentRoom.ToString().Contains(ExitDirection.E.ToString() ) && (currentCol == (currentColSize - 1) ) ) ||
                     (currentRoom.ToString().Contains(ExitDirection.S.ToString() ) && (currentRow == (currentRowSize - 1) ) ) || 
                     (currentRoom.ToString().Contains(ExitDirection.W.ToString() ) && (currentCol == 0 ) ) )
                {
                    currentRoom = RoomType.CLR;
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

    private int DebugPrintDungeonArray()
    {
        int numClrRooms = 0;

        string outputString = "";

        for (int row = 0; row < currentRowSize; row++)
        {
            for (int col = 0; col < currentColSize; col++)
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
