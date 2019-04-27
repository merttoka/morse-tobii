using System;
using System.Windows.Input;
using System.Threading.Tasks;
using Tobii.Interaction;
using Tobii.Interaction.Framework;
using System.Collections.Generic;

/// TODO:
// - Take into account of looking outside of the screen
// - Too long blinks (for example: 5 unit blink)
// - IMPLEMENT STATE MACHINE 

/// TIMING RULES
// DOT                           = 1 unit
// DASH                          = 3 units
// SPACE (SP1) (in same letter)  = 1 unit
// SPACE (SP2) (between letters) = 3 units
// SPACE (SP3) (between words)   = 7 units

namespace MorseCodeX
{
    class Program
    {
        // tobii device
        private static Host tobiiHost;

        // user parameters
        private static bool userPresense = true;

        // rythm parameters
        private static float dot = 1000.0f;     // time unit (ms)
        private static float c = 0.75f;          // scaling
        private static float EP = 50f;          // epsilon (ms)

        // dictionary
        private static Dictionary<String, String> alphabet = new Dictionary<String, String>()
        {
            { ".----", "1" }, { "..---", "2" }, {"...--", "3"}, {"....-", "4"}, {".....", "5"},
            { "-....", "6"}, {"--...", "7"}, {"---..", "8"}, {"----.", "9"}, {"-----", "0"},
            { ".-", "A"}, {"-...", "B"}, {"-.-.", "C"}, {"-..", "D"}, {".", "E"}, {"..-.", "F"},
            { "--.", "G"}, {"....", "H"}, {"..", "I"}, {".---", "J"}, {"-.-", "K"}, {".-..", "L"},
            { "--", "M"}, {"-.", "N"}, {"---", "O"}, {".--.", "P"}, {"--.-", "Q"}, {".-.", "R"},
            { "...", "S"}, {"-", "T"}, {"..-", "U"}, {"...-", "V"}, {".--", "W"}, {"-..-", "X"},
            { "-.--", "Y"}, {"--..", "Z"}
        };

        // current encoding
        private static String currentLetterEncoded = "";
        private static String currentWordDecoded = "";

        static String GetLetter(String morse) {
            if (alphabet.ContainsKey(morse))
                return alphabet[morse];
            else
                return "#";
        }

        static void Spaces(int counter) {
            // spaces in between symbols
            if (counter >= 7)
            {
                currentWordDecoded += GetLetter(currentLetterEncoded);
                Console.WriteLine($" {{{counter} [E.W.] || {currentWordDecoded}}}\n____________________________\n\n");
                currentLetterEncoded = "";
                currentWordDecoded = "";
            }
            else if (counter >= 3)
            {
                currentWordDecoded += GetLetter(currentLetterEncoded);
                Console.WriteLine($" {{{counter} [E.L.] || {currentWordDecoded}}}\n");
                currentLetterEncoded = "";
            }
            else if (counter >= 1)
            {
                Console.Write(" ");
            }
        }

        static void Main(string[] args)
        {
            tobiiHost = new Host();

            Console.WriteLine($"Eye tracking started. Blink to start.\n - Unit time:\t{c*dot/1000}s\n - Bpm:\t\t{60000/(c*dot)}\n - Offset:\t{EP/1000.0}s\n===========================\n");

            var userPresenseObserver = tobiiHost.States.CreateUserPresenceObserver();
            var gazePointDataStream = tobiiHost.Streams.CreateGazePointDataStream();

            // detects user 
            userPresenseObserver.WhenChanged(presenseState =>
            {
                if (presenseState.Value == UserPresence.Present)
                {
                    userPresense = true;
                }
                else if (presenseState.Value == UserPresence.Present)
                {
                    userPresense = false;
                }
                else
                {
                    userPresense = false;
                }
            });
            
            // last timestamp when eyelids are open
            double lastGazeTimestamp = 0;

            // last timestamp when eyelids are closed (for measuring spaces)
            double lastDotTimestamp = 0;

            // last timestamp when eyelids are open (for measuring spaces)
            double lastSpaceTimestamp = 0;

            // whether we are ready for a new dot or a dash 
            bool readyForNewSymbol = true;

            // counts consequtive spaces
            int spaceCounter = 0;

            gazePointDataStream.GazePoint((x, y, ts) => {
                if (userPresense)
                {
                    // initialize lasttimestamp
                    if (lastGazeTimestamp == 0 || lastSpaceTimestamp == 0) {
                        lastGazeTimestamp = ts;
                        lastSpaceTimestamp = ts;
                    }

                    if (spaceCounter >= 1) {
                        readyForNewSymbol = true;
                    }

                    // MORSE LETTERS
                    double diff = ts - lastGazeTimestamp;
                    if (readyForNewSymbol) {
                        // symbol timing
                        if (diff > 3 * c * dot - EP)
                        {
                            Spaces(spaceCounter);

                            Console.Write("-");
                            currentLetterEncoded += "-";

                            lastDotTimestamp = ts;
                            readyForNewSymbol = false;
                            spaceCounter = 0;
                        }
                        else if (diff > 1 * c * dot - EP)
                        {
                            Spaces(spaceCounter);

                            Console.Write(".");
                            currentLetterEncoded += ".";

                            lastDotTimestamp = ts;
                            readyForNewSymbol = false;
                            spaceCounter = 0;
                        }
                    }

                    // space calculator
                    if (lastDotTimestamp > 0) {
                        double diff_space = lastGazeTimestamp - Math.Max(lastDotTimestamp, lastSpaceTimestamp);
                        if (diff_space > 1 * c * dot - EP)
                        {
                            lastSpaceTimestamp = ts;
                            spaceCounter++;
                        }
                    }

                    lastGazeTimestamp = ts;
                }
            });

            Console.ReadKey();

            // we will close the coonection to the Tobii Engine before exit.
            tobiiHost.DisableConnection();
        }
    }
}
