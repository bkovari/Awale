using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Awale
{
    class Game
    {

        Player human;
        Robot machine;

        public static readonly int PointToWin = 25;
        public static readonly int PointToDraw = 24;
        public static readonly int HousePerTeam = 6;
        public static readonly int DefaultSeedPerHouse = 4;
        public static readonly int DeadLockPointLimit = 18;

        int[] tablehouses;
        int selectedHouseSeedCount;

        internal Player Human { get => human; set => human = value; }
        internal Robot Machine { get => machine; set => machine = value; }
        public int[] Tablehouses { get => tablehouses; set => tablehouses = value; }

        public Game(Player human, Robot machine)
        {
            this.Human = human;
            this.Machine = machine;
            this.Tablehouses = new int[HousePerTeam*2];

        }

        public void CommonHouseTableCopy(object individual) {

            if (individual is Player)
            {
                /* Create common housetable for player point calculation: 0..6 player */
                int j = HousePerTeam;
                for (int i = 0; i < HousePerTeam; i++)
                {
                    Tablehouses[i] = human.House[i];
                    Tablehouses[j] = machine.House[i];
                    j++;
                }
            }
            else {

                /* Create common housetable for robot point calculation: 0..6 robot */
                int j = HousePerTeam;
                for (int i = 0; i < HousePerTeam; i++)
                {
                    Tablehouses[i] = machine.House[i];
                    Tablehouses[j] = human.House[i];
                    j++;
                }
            }
        }

        private void IndividualHouseCopy(object lastParticipant) {

            /* Break down the common table into individual tables */
            // NOTE: lastParticipant 0..6 index always
            if (lastParticipant is Player)
            {
                int j = HousePerTeam;
                for (int i = 0; i < human.House.Length; i++)
                {
                    human.House[i] = tablehouses[i];
                    machine.House[i] = tablehouses[j];
                    j++;
                }
            }
            else
            {
                int j = HousePerTeam;
                for (int i = 0; i < human.House.Length; i++)
                {
                    machine.House[i] = tablehouses[i];
                    human.House[i] = tablehouses[j];
                    j++;
                }

            }
        }

        private void GainCountMonitor(object currentParticipant, int choice)
        {

            int count = 0;
            int _position = choice;
            int _seedcount = selectedHouseSeedCount;
            int _circleIdx = _position + _seedcount;

            // Overindex check (restart indexing if overindexed)
            if (_position + _seedcount >= Tablehouses.Length) { _circleIdx %= tablehouses.Length;  }

            // Condition whether the step is point eligible: 2 or 3 field and on opponent side
            bool _pointeligible = (tablehouses[_circleIdx] == 2) || (tablehouses[_circleIdx] == 3) && _circleIdx >= tablehouses.Length / 2;
            Trace.WriteLine("Pointeligible: " + _pointeligible);
            if ( _pointeligible ) {
                    int i = _circleIdx;
                
                    while ( (i >= tablehouses.Length/2 ) && (tablehouses[i] == 2 || tablehouses[i]  == 3) ) {
                    count += tablehouses[i];
                        tablehouses[i] = 0;
                        i--;
                    }
                }

            /* Credit the acquired gains if any */
                if (currentParticipant is Player)
                {
                    human.Gain = count;
                }
                else
                    machine.Gain = count;
        }

        public void PerformStep(object currentParticipant)
        {
            /* Create common house table to calculate point */
            CommonHouseTableCopy(currentParticipant);
            int _choice = (currentParticipant is Player) ? human.Currentchoice : machine.Currentchoice;

            /* Follow selected house seed count */
            selectedHouseSeedCount = (currentParticipant is Player) ? human.House[_choice] : machine.House[_choice];

            /* Perform current participant's step in common house table */
            int _seedcount = selectedHouseSeedCount;
            int _position = _choice;
            int _offset = 1;
            Tablehouses[_position] = 0;
            while (_seedcount > 0) {
                if (_position + _offset == _choice) { _offset++; continue; } // Skip selected house after a turn
                if (_position + _offset >= Tablehouses.Length) { _position = 0; _offset = 0; } // Overindex check
                Tablehouses[_position+_offset] += 1;
                _seedcount--;
                _offset++;
            }

            /* Check if participant earned any point */
            GainCountMonitor(currentParticipant, _choice);

            /* Break down the common table into individual tables based on actual step */
            IndividualHouseCopy(currentParticipant);            
        }

        public bool IsGameDraw() {
            return (human.Point > Game.DeadLockPointLimit  && machine.Point > Game.DeadLockPointLimit) ? true : false;
        }

        public bool IsGameWon() {
            return ( (human.Point >= 25) || (machine.Point >= 25) ) ? true : false;
        }

        public void DisplayFinalResult() {

            if (this.IsGameDraw()) {
                Console.WriteLine("The game has come to a deadlock, result is draw!");
            }
            else
            {
                string _winner = (human.Point > machine.Point) ? human.Name : "Robot";
                Console.WriteLine(_winner.ToUpper() + " has won the game!");
            }
        }

        private void PrintRobotStatus()
        {
            Console.Clear();
            Console.Write("[ROBOT (Point: " + machine.Point.ToString() + ") ]\t\t");
            for (int i = machine.House.Length-1; i > -1; i--)
            {
                Console.Write(" | ");
                for (int j = machine.House[i]-1; j > -1; j--)
                {
                    Console.Write("O");
                }
            }
            Console.Write(" |\n");
        }

        private void PrintPlayerStatus()
        {
            Console.Write("[" + human.Name.ToUpper() + " (Point: " + human.Point.ToString() +") ]\t\t");
            for (int i = 0; i < human.House.Length; i++)
            {
                Console.Write(" | ");
                for (int j = 0; j < human.House[i]; j++)
                {
                    Console.Write("O");
                }
            }
            Console.Write(" |\n\n");
        }

        public void PrintGameTable() {
            PrintRobotStatus();
            Console.WriteLine(String.Concat(Enumerable.Repeat("=",80)));
            PrintPlayerStatus();
        }


        public static string AskPlayerName()
        {
            string playername = "";
            do
            {
                try
                {
                    Console.WriteLine("\nPlease give me your name: ");
                    playername = Console.ReadLine();
                }
                catch (Exception)
                {
                    Console.WriteLine("Wrong input, try again!");
                }

                if (playername.Length < 3 || playername.Length > 9)
                {
                    Console.WriteLine("Name must be between 3 to 9 characters!\n");
                }

            } while (playername.Length < 3 || playername.Length > 9);
  
            return playername;
        }
        public  int AskPlayerChoice()
        {

            int housechoice = 0;
            do
            {
                try
                {
                    Console.WriteLine("HUMAN: Select any of your non empty houses " + human.GetNonEmptyHouseList());
                    housechoice = (int.Parse(Console.ReadLine()) - 1);

                    if (!(housechoice > -1 && housechoice < 6)) { Console.WriteLine("Wrong input, try again!"); }
                }
                catch (Exception)
                {
                    Console.WriteLine("Wrong input, try again!");
                }

            } while (!(housechoice > -1 && housechoice < Game.HousePerTeam));

            return housechoice;
        }

    }

    class Player {

        int[] house;
        int point;
        int currentchoice;
        int gain;
        string name;

        public int[] House { get => house; set => house = value; }
        public int Point { get => point; set => point = value; }
        public string Name { get => name; set => name = value; }
        public int Currentchoice { get => currentchoice; set => currentchoice = value; }
        public int Gain { get => gain; set => gain = value; }

        public Player(string name) {
            this.Name = name;
            point = 0;
            gain = 0;

            /* Set default house setup */
            house = new int[Game.HousePerTeam];
            for (int i = 0; i < house.Length; i++)
            {
                house[i] = Game.DefaultSeedPerHouse; // Set default seed count
            }

        }

        public void GainPointIfEarned() {

            if(gain > 0)
            {
                point += gain;
                gain = 0;
            }
        }

        public bool IsChoiceValid()
        {
            return (house[currentchoice] == 0) ? false : true;
        }

        public string GetNonEmptyHouseList() {

            string houselist = "[";

            for (int i = 0; i < house.Length; i++)
            {
                if (house[i] != 0)
                {
                    
                    houselist += (i+1).ToString();
                    if (i == house.Length-1) { continue;  }
                    houselist += ",";

                }
            }

            houselist += "]";

            return houselist;

        }

    }

    class Robot
        {

        int[] house;
        int point;
        int currentchoice;
        int gain;

        public int[] House { get => house; set => house = value; }
        public int Point { get => point; set => point = value; }
        public int Currentchoice { get => currentchoice; set => currentchoice = value; }
        public int Gain { get => gain; set => gain = value; }

        public static readonly int CongestionLimit = Game.DefaultSeedPerHouse * 2;

        public Robot()
            {
                point = 0;
                gain = 0;

                /* Set default house setup */
                house = new int[Game.HousePerTeam];
                for (int i = 0; i < house.Length; i++)
                {
                    house[i] = Game.DefaultSeedPerHouse; // Set default seed count
                }
            }

        public void GainPointIfEarned()
        {

            if (gain > 0)
            {
                point += gain;
                gain = 0;
            }
        }


        private int CongestionCheck() {

            // -1 if no congestion
            // item index if congestion

            int congestionIdx = -1;
            for (int i = 0; i < house.Length; i++)
            {
                if (house[i] > Robot.CongestionLimit) { congestionIdx = i; }
            }

            return congestionIdx;
        }

        public int MakeBestChoice(Game game) {

            // Create common table for robot point calculation
            game.CommonHouseTableCopy(game.Machine);

            /* First try avoid any congestions: choose congestion likely field  */
            int _congestionResult = CongestionCheck();
            if (_congestionResult > 0) {

                // Congestion field found
                Trace.WriteLine("Congestion likely field selected: " + _congestionResult);
                return _congestionResult;
            }

            int _bestchoice = 0;
            int[] _indicesOfPossibleTargets = new int[Game.HousePerTeam];

            /* Make a table with indexes of 1 or 2 seed opponent places in common table */
            for (int i = 0; i < Game.HousePerTeam; i++)
            {
                // Set -1 dummy indexes to be able to use the table after 
                _indicesOfPossibleTargets[i] = -1;
            }
            int cnt = 0;
            for (int i = game.Human.House.Length; i < game.Tablehouses.Length; i++)
            {
                if (game.Tablehouses[i] == 1 || game.Tablehouses[i] == 2 ) {
                    _indicesOfPossibleTargets[cnt] = i;
                    cnt++;
                }

            }

            /* Make distance count table for each possible (1 or 2 seed) elements */
            int j = 0;
            int[] _seedCountOfPossibleTargets = new int[Game.HousePerTeam];
            for (int i = 0; i < game.Human.House.Length; i++)
            {
                while (_indicesOfPossibleTargets[j] != -1)
                {
                    if (i + game.Tablehouses[i] >= _indicesOfPossibleTargets[j]) {
                        _seedCountOfPossibleTargets[i] += (game.Tablehouses[_indicesOfPossibleTargets[j]] + 1 );
                    }
                    j++;
                }
                j = 0;
            }

            /* Choose the first index of max from the seedcount table (the most valuable) */
            int maxIdx = _seedCountOfPossibleTargets[0];
            for (int i = 0; i < _seedCountOfPossibleTargets.Length; i++)
            {
                if(_seedCountOfPossibleTargets[i] >= maxIdx) { maxIdx = i; }
            }

            _bestchoice = maxIdx;

            Trace.Write("----------- ROBOT STEP DETAILS ----------\n");

            /* If there is no pointgaining step, pick the most populated non empty */
            if (Robot.GetSumOfArray(_seedCountOfPossibleTargets) == 0) {
                
                int _alternatechoice = GetMaxSeedCountHouseIndex(GetNonEmptyHouseIndexes());
                Trace.WriteLine("No best case, house pick: " + _alternatechoice);
                return _alternatechoice;
            }

            Trace.WriteLine("Optimal step found, pick: " + _bestchoice);          
            return _bestchoice;

        }

        public void PretendThinking()
        {

            Random rnd = new Random();
            int _thinkingTime = rnd.Next(1500, 3001);
            Console.WriteLine("ROBOT: Thinking...");
            Thread.Sleep(_thinkingTime);
        }

        private int[] GetNonEmptyHouseIndexes() {

            int[] nonemptyHouseIndexes = new int[house.Length];

            int count = 0;
            Trace.Write("Non empty house indexes: ");
            for (int i = 0; i < house.Length; i++)
            {
                if (house[i] != 0)
                {                
                    nonemptyHouseIndexes[count] = i;
                    Trace.Write(nonemptyHouseIndexes[count]);
                    count++;

                }
            }
            Trace.Write("\n");
            return nonemptyHouseIndexes;
        }

        private static int GetSumOfArray(int[] anyarray) {

            int sum = 0;
            for (int i = 0; i < anyarray.Length; i++)
            {
                sum += anyarray[i];
            }

            return sum;
        }

        private int GetMaxSeedCountHouseIndex(int[] nonemptyhouseIndexArray) {

            int maxIdx = 0;
            for (int i = 0; i < house.Length; i++)
            {
                if (house[nonemptyhouseIndexArray[i]] > house[maxIdx]) { maxIdx = nonemptyhouseIndexArray[i]; }
            }

            return maxIdx;
        }

    }

    class Program
    {
        static string PlayerName = "";
        static void PrintWelcome() {

            string welcomemessage = "---------------------- WELCOME TO AWALE GAME -----------------\n";
            Console.WriteLine(welcomemessage);

        }
        static void AskIfReadyToPlay() {

            char choice = ' ';

            /* Check whether the player is up to start a game */
            do
            {
                Console.WriteLine("Ready to start a game? [y/n]");
                try
                {
                    choice = (Console.ReadKey().KeyChar);
                }
                catch (Exception)
                {
                    Console.WriteLine("\nWrong input, try again!");
                }

                if ( !(char.ToLower(choice) == 'y' || char.ToLower(choice) == 'n') )
                {
                    Console.WriteLine("\nWrong input, try again!");
                }

                if (char.ToLower(choice) == 'n') { Environment.Exit(0); }

            } while (char.ToLower(choice) != 'y');

        }

        static string Trace_Tables(Game Awale) {

            string _commonhouseseeds = "";
            int sum = 0;
            for (int i = 0; i < Awale.Tablehouses.Length; i++)
            {
                _commonhouseseeds += " " + Awale.Tablehouses[i] + "";
                sum += Awale.Tablehouses[i];
                
            }
            _commonhouseseeds += " Seedsum: [" + sum + "]";

            string _machinehouseseeds = "";
            for (int k = Awale.Machine.House.Length-1; k > -1; k--)
            {
                _machinehouseseeds += " " + Awale.Machine.House[k] + "";
            }


            string _humanhouseseeds = "";
            for (int j = 0; j < Awale.Human.House.Length; j++)
            {
                _humanhouseseeds += " " + Awale.Human.House[j] + "";
            }

            return "Commonhouse seeds: \t" + _commonhouseseeds + "\n" +
                   "Robothouse seeds: \t" + _machinehouseseeds + "\n" +
                   "Humanhouse seeds: \t" + _humanhouseseeds + "\n";
        }

        static void PlayGame() {

            /* Get username and start the game */
            if (Program.PlayerName == "") { Program.PlayerName = Game.AskPlayerName(); } 
            Game Awale = new Game(new Player(Program.PlayerName), new Robot());
            Awale.PrintGameTable();

            /* Play until somebody wins or draw */
            while ((!Awale.IsGameDraw() && !Awale.IsGameWon()))
            {

                /* Get user choice */
                do
                {
                    Awale.Human.Currentchoice = Awale.AskPlayerChoice();
                    if (!Awale.Human.IsChoiceValid()) { Console.WriteLine("You're choice is not valid, try again!"); }

                } while (!Awale.Human.IsChoiceValid());

                /* Perform human step, acquire point if any, refresh table */
                Awale.PerformStep(Awale.Human);
                Awale.Human.GainPointIfEarned();
                Awale.PrintGameTable();
                // Trace house seeds
                Trace.WriteLine("------------- HUMAN STEP DETAILS --------");
                Trace.WriteLine(Program.Trace_Tables(Awale));
                Trace.WriteLine("Human point: " + Awale.Human.Point);

                if (!Awale.IsGameDraw() && !Awale.IsGameWon()) // When player won, no robot step
                {
                    /* Determine best robot step, perform, acquire point if any, refresh table */
                    Awale.Machine.PretendThinking();
                    Awale.Machine.Currentchoice = Awale.Machine.MakeBestChoice(Awale);
                    Awale.PerformStep(Awale.Machine);
                    Awale.Machine.GainPointIfEarned();
                    Awale.PrintGameTable();
                    // Trace house seeds 
                    Trace.WriteLine(Program.Trace_Tables(Awale));
                    Trace.WriteLine("Robot point: " + Awale.Machine.Point);
                }

            }
            Awale.DisplayFinalResult();
            Program.AskIfReadyToPlay();

        }

        static void Main(string[] args)
        {
            Program.PrintWelcome();
            Program.AskIfReadyToPlay();

            do
            {
                Program.PlayGame();
            } while (true);

        }
    }
}
