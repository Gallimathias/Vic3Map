﻿
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq.Expressions;

internal class Program
{
    private static void Main(string[] args) {
        //check if .net framework 6.0 is installed
        if (System.Environment.Version.Major < 6) {
            Console.WriteLine("This program requires .NET Framework 6.0 or higher. Please install it and try again.");
            Console.ReadKey();
            return;
        }
        Stopwatch sw = Stopwatch.StartNew();
        Random rand = new Random();
        //move up 3 directorys from local
        string localDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        
        //Draws Individual RGO Maps
        bool doDrawRGOs = false;
        //Draws National Maps as they woud exist at the start of the game
        bool doDrawStartingNations = true;
        //Draws National Maps as they exist in the provided saves (Dynamic Tags Buggy)
        bool doDrawSaves = true;
        //Should Decentralized Nations be drawn on the National Maps
        bool doDrawDecentralized = true;

        List<(List<string> rgoNames, Color hColor, Color tColor)> rgoColors = new List<(List<string>, Color, Color)>();        

        //check input config file
        if (!File.Exists(localDir + "/_Input/input.cfg")) {
            Console.WriteLine("Error: input.cfg not found, using defaults");
        }
        else {
            inputCFG();
        }


        List<State> stateList = new List<State>();
        parseStateFiles(stateList);
        List<Region> regionList = new List<Region>();
        parseRegionFiles(regionList);
        mergeStateRegion(stateList, regionList);

        writeRGOs(regionList);
        //debugStateProv(regionList);


        parseDefaultMap(regionList);
        
        parseProvMap(regionList);


        //method to parse state files
        void parseStateFiles(List<State> stateList) {
            //read all files in localDir/_Input/state_regions
            string[] files = Directory.GetFiles(localDir + "/_Input/map_data/state_regions");
            //for each file
            int count = 0;

            foreach (string file in files) {
                if (file.EndsWith(".txt")) {
                    //read file
                    string[] lines = File.ReadAllLines(file);
                    //for each line
                    //Console.WriteLine(file);
                    State s = new State();
                    Resource dr = new Resource();
                    bool cappedResourseFound = false;
                    bool discoverableResourseFound = false;
                    bool traitsfound = false;
                    foreach (string l1 in lines) {
                        string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                        //get STATE_NAME
                        if (line.StartsWith("STATE_")) {
                            //Console.WriteLine("\t"+line.Split()[0]);
                            s = new State(line.Split()[0]);

                            //incase people are orverriding states in latter files
                            //check if state with same name already exists in stateList and if so, delete it
                            foreach (State state in stateList) {
                                if (state.name == s.name) {
                                    stateList.Remove(state);
                                    break;
                                }
                            }

                            stateList.Add(s);
                        }
                        //get stateID
                        if (line.StartsWith("id")) {
                            s.stateID = int.Parse(line.Split()[2]);
                        }
                        if (line.StartsWith("subsistence_building")) {
                            s.subsistanceBuilding = line.Split("=")[1].Replace("\"", "").Trim();
                        }

                        //get provinces
                        if (line.TrimStart().StartsWith("provinces")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    s.provIDList.Add(n);
                                    s.addProv(n);
                                }
                            }
                            
                        }
                        //get impassable colors
                        if (line.TrimStart().StartsWith("impassable")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    Color c = ColorTranslator.FromHtml("#" + n);
                                    //set prov with color c to isImpassible
                                    if (s.provDict.TryGetValue(c, out Province p)) {
                                        p.isImpassible = true;
                                    }

                                }
                            }
                        }
                        //get prime_land colors
                        if (line.TrimStart().StartsWith("prime_land")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    Color c = ColorTranslator.FromHtml("#" + n);
                                    //set prov with color c to prime land
                                    if (s.provDict.TryGetValue(c, out Province p)) {
                                        p.isPrimeLand = true;
                                    }
                                }
                            }
                        }

                        //get traits
                        if (line.Trim().StartsWith("traits")) {
                            traitsfound = true;
                        }
                        if (traitsfound) {
                            string[] l2 = line.Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"")) {
                                    s.traits.Add(l2[i].Replace("\"", ""));
                                }
                            }
                        }

                        //get arable_land
                        if (line.TrimStart().StartsWith("arable_land")) {
                            s.arableLand = int.Parse(line.Split("=")[1].Trim());
                            count++;
                        }
                        //get arable_resources
                        if (line.TrimStart().StartsWith("arable_resources")) {
                            string[] resList = line.Split("=")[1].Replace("\"", "").Split(" ");
                            for (int i = 0; i < resList.Length; i++) {
                                if (resList[i].StartsWith("bg_")) {
                                    Resource r = new Resource(resList[i]);
                                    r.knownAmmount = s.arableLand;
                                    r.type = "agriculture";
                                    s.resoures.Add(r);
                                }
                            }
                        }
                        //get capped_resources
                        if (line.TrimStart().StartsWith("capped_resources")) {
                            cappedResourseFound = true;
                        }
                        if (cappedResourseFound) {
                            if (line.TrimStart().StartsWith("bg_")) {
                                string[] l2 = line.Replace("\"", "").Split("=");
                                Resource r = new Resource(l2[0].Trim());
                                r.knownAmmount = int.Parse(l2[1].Trim());
                                r.type = "resource";
                                s.resoures.Add(r);
                            }
                        }
                        //get discvorable resources
                        if (line.TrimStart().StartsWith("resource")) {
                            discoverableResourseFound = true;
                        }
                        if (discoverableResourseFound) {

                            if (line.TrimStart().StartsWith("type")) {
                                string[] l2 = line.Split("=");
                                dr = new Resource(l2[1].Trim().Replace("\"", ""));
                                dr.type = "discoverable";
                                s.resoures.Add(dr);
                            }
                            else if (line.TrimStart().StartsWith("undiscovered_amount")) {
                                string[] l2 = line.Split("=");
                                dr.discoverableAmmount = int.Parse(l2[1].Trim());
                            }
                            else if (line.TrimStart().StartsWith("amount") || line.TrimStart().StartsWith("discovered_amount")) {
                                string[] l2 = line.Split("=");
                                dr.knownAmmount = int.Parse(l2[1].Trim());
                            }
                        }
                        //get naval id
                        if (line.TrimStart().StartsWith("naval_exit_id")) {
                            string[] l2 = line.Split("=");
                            s.navalID = int.Parse(l2[1].Trim());
                        }

                        //get city color
                        if (line.TrimStart().StartsWith("city") || line.TrimStart().StartsWith("port") || line.TrimStart().StartsWith("farm") || line.TrimStart().StartsWith("mine") || line.TrimStart().StartsWith("wood")) {

                            Color hubC = ColorTranslator.FromHtml("#" + line.Split("=")[1].Replace("\"", "").Replace("x", "").Trim());
                            //set prov with color c hubName to name
                            if (s.provDict.TryGetValue(hubC, out Province p)) {
                                p.hubName = line.Split("=")[0].Trim();
                                //if s.color.A is 0 set the color to hubcolor
                                if (s.color.A == 0) {
                                    s.color = p.color;
                                }
                            }
                        }
                        //reset cappedResourseFound and discoverableResourseFound
                        if (line.Trim().StartsWith("}")) {
                            cappedResourseFound = false;
                            discoverableResourseFound = false;
                            traitsfound = false;
                        }

                    }
                }
            }

            Console.WriteLine("States: " + count + " | " + stateList.Count);

        }
        //parse all region files
        void parseRegionFiles(List<Region> regionList) {
            string[] files = Directory.GetFiles(localDir + "/_Input/common/strategic_regions");

            int count = 0;
            foreach (string file in files) {
                if (file.EndsWith(".txt")) {
                    string[] lines = File.ReadAllLines(file);
                    Region r = new Region();
                    //Console.WriteLine(file);
                    foreach (string l1 in lines) {
                        string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                        if (line.Trim().StartsWith("region_")) {
                            r = new Region(line.Split("=")[0].Trim());

                            //incase people are orverriding regions in latter files
                            //check if region with same name already exists in regionList and if so, delete it
                            foreach (Region region in regionList) {
                                if (region.name == r.name) {
                                    regionList.Remove(region);
                                    break;
                                }
                            }

                            regionList.Add(r);
                        }
                        else if (line.Trim().StartsWith("states")) {
                            string[] states = line.Split("=")[1].Replace("{", "").Replace("}", "").Split(" ");
                            for (int i = 0; i < states.Length; i++) {
                                if (states[i].StartsWith("STATE_")) {
                                    r.stateNames.Add(states[i]);
                                }
                            }
                        }
                        else if (line.Trim().StartsWith("map_color")) {
                            count++;
                            string[] e = line.Split("=")[1].Split(" ");

                            List<double> rgbValues = new List<double>();
                            
                            foreach (string s in e) {
                                //try parse float
                                if (double.TryParse(s, out double d)) {
                                    //if d is between 0 and 1.1 then multiply it by 255
                                    if (d > 0 && d < 1.1) {
                                        d = d * 255;
                                    }
                                    //if d is outsied of 0-255 range, then set it to 0 or 255
                                    if (d < 0) {
                                        d = 0;
                                    }
                                    else if (d > 255) {
                                        d = 255;
                                    }
                                    
                                    rgbValues.Add(d);
                                }
                            }
                            //if rgbValues has less than 3 values, then add 128 to make it 3
                            while (rgbValues.Count < 3) {
                                rgbValues.Add(128);
                            }
                            
                            r.color = Color.FromArgb((int)rgbValues[0], (int)rgbValues[1], (int)rgbValues[2]);

                        }
                        else if (line.StartsWith("graphical_culture")){
                            r.gfxCulture = line.Split("=")[1].Replace("\"", "").Trim();
                        }
                    }
                }
            }
            Console.WriteLine("Regions: " + count + " | " + regionList.Count);
        }


        //merge state into regions
        void mergeStateRegion(List<State> stateList, List<Region> regionList) {
            foreach (Region r in regionList) {
                foreach (State s in stateList) {
                    if (r.stateNames.Contains(s.name)) {
                        r.states.Add(s);
                        s.hexToColor();
                    }
                }
            }
        }

        //parse default.map
        void parseDefaultMap(List<Region> regionList) {
            //dictionary of color and prov object
            Dictionary<Color, Province> colorToProvDic = new Dictionary<Color, Province>();

            //itterate throu all states in regionList and add their provDict to colorToProv
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (KeyValuePair<Color, Province> kvp in s.provDict) {
                        colorToProvDic.Add(kvp.Key, kvp.Value);
                    }
                }
            }



            string[] lines = File.ReadAllLines(localDir + "/_Input/map_data/default.map");
            bool seaStart = false;
            bool lakeStart = false;
            foreach (string line in lines) {
                if (line.Trim().StartsWith("sea_starts")) {
                    seaStart = true;
                }
                else if (line.Trim().StartsWith("lakes")) {
                    lakeStart = true;
                }
                if (seaStart) {
                    string[] l2 = line.Trim().Split(" ");
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        else if (l2[i].StartsWith("x")) {
                            //set prov with that color to sea in colorToProvDic
                            foreach (KeyValuePair<Color, Province> kvp in colorToProvDic) {
                                if (kvp.Key.ToArgb() == ColorTranslator.FromHtml("#" + l2[i].Replace("x", "")).ToArgb()) {
                                    kvp.Value.isSea = true;
                                    break;
                                }
                            }
                        }
                        else if (l2[i].StartsWith("}")) {
                            seaStart = false;
                            break;
                        }
                    }
                }
                if (lakeStart) {
                    string[] l2 = line.Trim().Split(" ");
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        else if (l2[i].StartsWith("x")) {
                            //similary for lakes
                            foreach (KeyValuePair<Color, Province> kvp in colorToProvDic) {
                                if (kvp.Key.ToArgb() == ColorTranslator.FromHtml("#" + l2[i].Replace("x", "")).ToArgb()) {
                                    kvp.Value.isLake = true;
                                    break;
                                }
                            }
                        }
                        else if (l2[i].StartsWith("}")) {
                            seaStart = false;
                            break;
                        }
                    }
                }
            }
        }

        //pares province png
        void parseProvMap(List<Region> regionList) {
            //dictionary prov color to state object
            Dictionary<Color, Province> provColorToProv = new Dictionary<Color, Province>();

            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    //add all prov colors to provColorToProv
                    foreach (KeyValuePair<Color, Province> kvp in s.provDict) {
                        provColorToProv.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            
            Bitmap image = new Bitmap(localDir + "/_Input/map_data/provinces.png");
            Bitmap provBorder = new Bitmap(image.Width, image.Height);

            Console.WriteLine("Parsing Map");
            //parse image and get coords of each color and add them to the state and draw borders
            for (int i = 0; i < image.Width; i++) {                
                Color lastColor = image.GetPixel(i, 0);
                for (int j = 0; j < image.Height; j++) {
                    Color c = image.GetPixel(i, j);
                    //if c is in provColorToProv add coord to state
                    if (provColorToProv.ContainsKey(c)) provColorToProv[c].coordList.Add((i, j));
                    if (c != lastColor) {
                        provBorder.SetPixel(i, j, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 0% and 50% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + i * 100 / image.Width / 2 + "%");
                }

            }

            //draw vertical borders
            //Console.WriteLine("Drawing Vertical Borders for Province Map");
            for (int i = 0; i < image.Height; i++) {
                Color lastColor = image.GetPixel(0, i);
                for (int j = 1; j < image.Width; j++) {
                    Color c = image.GetPixel(j, i);
                    if (c != lastColor) {
                        provBorder.SetPixel(j, i, Color.Black);
                        lastColor = c;
                    }
                }
                //progress bar every 25% with 0% and 100% mapping to 50% and 100% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + (i * 100 / image.Width / 2 + 50) + "%");
                }

            }

            //set coordList for each state
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    s.setCoords();
                }
            }

            //check if /_Output/BorderFrame exists if not add it
            if (!Directory.Exists(localDir + "/_Output/BorderFrame")) {
                Directory.CreateDirectory(localDir + "/_Output/BorderFrame");
            }

            //save map
            provBorder.Save(localDir + "/_Output/BorderFrame/prov_border.png");

            drawStateImages(regionList, image);
            List<(int, int)> waterCoordList = darwRegionImages(regionList, image);


            if (doDrawRGOs) {
                ((int, int) waterRecCenter, (int, int) waterRecSize) = drawRGOMaps(regionList, waterCoordList);

                mergeMaps();
                namedMapes(regionList);

                debugDrawRectangle(regionList, waterRecCenter, waterRecSize);

                drawHubs(regionList, image);
                drawImapssiblePrime(regionList, image);
            }

            

            if (doDrawStartingNations || doDrawSaves) {
                Dictionary<string, Nation> nationDict = parseNations(regionList);
                if (nationDict == null) {
                    return;
                }

                if (doDrawStartingNations) {
                    drawNationsMap(nationDict, "Starting_National", doDrawDecentralized);
                }
                if (doDrawSaves) {
                    parseTerrain(regionList);
                    //for every file in _Input/Saves
                    foreach (string file in Directory.GetFiles(localDir + "/_Input/Saves")) {
                        //if file is a .txt file
                        if (file.EndsWith(".v3")) {
                            parseSave(regionList, nationDict, file);

                            //seprate name from file path
                            string[] split = file.Split('\\');
                            string fileName = split[split.Length - 1].Split(".")[0];

                            drawNationsMap(nationDict, fileName, doDrawDecentralized);
                        }
                    }

                    
                    
                    
                }
            }
            Console.WriteLine(sw.Elapsed);

        }
        
        //draw state images
        void drawStateImages(List<Region> regionList, Bitmap image) {
            Bitmap stateImage = new Bitmap(image.Width, image.Height);
            Bitmap stateBorder = new Bitmap(image.Width, image.Height);
            Console.WriteLine("Drawing State Maps");
            foreach (Region r in regionList) {                               
                foreach (State s in r.states) {
                    foreach ((int, int) c in s.coordList) {
                        stateImage.SetPixel(c.Item1, c.Item2, s.color);
                    }
                }
            }

            //draw horizontal borders for state map            
            for (int i = 0; i < image.Width; i++) {
                Color lastColor = stateImage.GetPixel(i, 0);
                for (int j = 0; j < image.Height; j++) {
                    Color c = stateImage.GetPixel(i, j);
                    if (c != lastColor) {
                        stateBorder.SetPixel(i, j, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 0% and 50% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + i * 100 / image.Width / 2 + "%");
                }
            }

            //draw vertical borders for state map
            for (int i = 0; i < image.Height; i++) {
                Color lastColor = stateImage.GetPixel(0, i);
                for (int j = 1; j < image.Width; j++) {
                    Color c = stateImage.GetPixel(j, i);
                    if (c != lastColor) {
                        stateBorder.SetPixel(j, i, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 50% and 100% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + (i * 100 / image.Width / 2 + 50) + "%");
                }
            }

            //check if /_Output/ColorMap exists if not add it
            if (!Directory.Exists(localDir + "/_Output/ColorMap")) {
                Directory.CreateDirectory(localDir + "/_Output/ColorMap");
            }

            //save state images
            stateImage.Save(localDir + "/_Output/ColorMap/state_colors.png");
            stateBorder.Save(localDir + "/_Output/BorderFrame/state_border.png");


        }

        //draw region images
        List<(int, int)> darwRegionImages(List<Region> regionList, Bitmap image) {
            Bitmap regionImage = new Bitmap(image.Width, image.Height);
            Bitmap regionBorder = new Bitmap(image.Width, image.Height);
            Bitmap waterImage = new Bitmap(image.Width, image.Height);

            List<(int, int)> waterCoordList = new List<(int, int)>();


            Console.WriteLine("Drawing Region Maps");
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach ((int, int) c in s.coordList) {
                        regionImage.SetPixel(c.Item1, c.Item2, r.color);
                    }
                }
            }

            //draw horizontal borders for region map            
            for (int i = 0; i < image.Width; i++) {
                Color lastColor = regionImage.GetPixel(i, 0);
                for (int j = 0; j < image.Height; j++) {
                    Color c = regionImage.GetPixel(i, j);
                    if (c.A == 0) {
                        waterCoordList.Add((i, j));
                        waterImage.SetPixel(i, j, Color.LightBlue);
                    }
                    if (c != lastColor) {
                        regionBorder.SetPixel(i, j, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 0% and 50% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + i * 100 / image.Width / 2 + "%");
                }
            }

            //draw vertical borders for region map
            for (int i = 0; i < image.Height; i++) {
                Color lastColor = regionImage.GetPixel(0, i);
                for (int j = 1; j < image.Width; j++) {
                    Color c = regionImage.GetPixel(j, i);
                    if (c != lastColor) {
                        regionBorder.SetPixel(j, i, Color.Black);
                        lastColor = c;
                    }
                }

                //progress bar every 25% with 0% and 100% mapping to 50% and 100% of total progress
                if (i % (image.Width / 4) == 0) {
                    Console.WriteLine("\t" + (i * 100 / image.Width / 2 + 50) + "%");
                }
            }

            //save region images
            regionImage.Save(localDir + "/_Output/ColorMap/region_colors.png");
            regionBorder.Save(localDir + "/_Output/BorderFrame/region_border.png");
            waterImage.Save(localDir + "/_Output/ColorMap/water_map.png");



            return waterCoordList;

        }

        //drawRGOMaps
        ((int, int) waterCenter, (int, int) waterMaxSize) drawRGOMaps(List<Region> regionList, List<(int, int)> waterCoordList) {
            //if Output/RGOs/ does not exist, create it
            if (!Directory.Exists(localDir + "/_Output/RGOs/")) {
                Directory.CreateDirectory(localDir + "/_Output/RGOs/");
            }

            List<string> rgoNames = setRGOColors(regionList);
            Bitmap image = new Bitmap(localDir + "/_Input/map_data/provinces.png");
            Bitmap water = new Bitmap(localDir + "/_Output/ColorMap/water_map.png");
            Bitmap stateBorder = new Bitmap(localDir + "/_Output/BorderFrame/state_border.png");

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for numbers and names

            //find the largest rectangle without holes in the water
            MaximumRectangle mr = new MaximumRectangle();
            ((int, int) waterCenter, (int, int) waterMaxRecSize) = ((0, 0), (0, 0));
            if (waterCoordList.Count > 0) {
                ( waterCenter, waterMaxRecSize) = mr.center(waterCoordList, false);
            }
            

            for (int i = 0; i < rgoNames.Count; i++) {
                string name = rgoNames[i];
                string wType = "";
                Color resColor = Color.FromArgb(255, 255, 255, 255);
                Color textColor = Color.FromArgb(255, 255, 255, 255);
                Console.WriteLine(name + "\t" + sw.Elapsed);
                Bitmap rgoMap = new Bitmap(image.Width, image.Height);
                Bitmap rgoName = new Bitmap(image.Width, image.Height);
                Graphics g = Graphics.FromImage(rgoMap);
                g.Clear(Color.White);
                g.DrawImage(water, Point.Empty);

                foreach (Region r in regionList) {
                    foreach (State s in r.states) {
                        foreach (Resource res in s.resoures) {
                            if (res.name.Contains(name)) {
                                //setpixel for each s.coords in rgoMap
                                foreach ((int, int) c in s.coordList) {
                                    rgoMap.SetPixel(c.Item1, c.Item2, res.color);
                                }
                            }
                        }
                    }
                }

                g.DrawImage(stateBorder, Point.Empty);

                foreach (Region r in regionList) {
                    foreach (State s in r.states) {
                        foreach (Resource res in s.resoures) {
                            if (res.name.Contains(name)) {
                                wType = res.type;
                                //write text  
                                string val = "";
                                if (res.type.Equals("agriculture")) {
                                    val = s.arableLand.ToString();
                                }
                                else {
                                    if (res.knownAmmount > 0) {
                                        val += res.knownAmmount;
                                    }
                                    if (res.discoverableAmmount > 0) {
                                        if (res.knownAmmount > 0) {
                                            val += "|";
                                        }
                                        val += "(" + res.discoverableAmmount + ")";
                                    }
                                }


                                bool gotRectangularBox = false;
                                if (val.Length > 4) { //for those cases where the number would look better in a long rectangle than a square
                                    s.getCenter2();
                                    gotRectangularBox = true;
                                    Console.WriteLine("\t" + res.name + " in " + s.name + " switching to rectange");
                                }


                                int numberFontSize = 8; //minimum font size for number
                                Font font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                                //check pixel size of font1
                                SizeF size1 = g.MeasureString(val, font1);
                                //if size1 is smaller than state maxRecSize then increase font size to fit
                                while (size1.Width < s.maxRecSize.Item1 && size1.Height < s.maxRecSize.Item2) {
                                    numberFontSize++;
                                    font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                                    size1 = g.MeasureString(val, font1);
                                }
                                numberFontSize = (int)(numberFontSize * 1.3);
                                font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                                if (numberFontSize < 20) {
                                    numberFontSize = 10;
                                    font1 = new Font("Verdana", numberFontSize);
                                    size1 = g.MeasureString(val, font1);
                                    while (size1.Width < s.maxRecSize.Item1 && size1.Height < s.maxRecSize.Item2) {
                                        numberFontSize++;
                                        font1 = new Font("Verdana", numberFontSize);
                                        size1 = g.MeasureString(val, font1);
                                    }
                                    numberFontSize = (int)(numberFontSize * 1.2);
                                    font1 = new Font("Verdana", numberFontSize, FontStyle.Bold);

                                }

                                resColor = res.color;
                                textColor = res.textColor;
                                g.DrawString(val, font1, new SolidBrush(res.textColor), new Point(s.center.Item1, s.center.Item2), stringFormat);

                                if (gotRectangularBox) { //revert back to square for the rest of the res in that state
                                    s.getCenter2(true);
                                }

                            }
                        }
                    }
                }

                List<string> tmpName = name.Replace("bg_", "").Split("_").ToList();
                string wName = "";
                for (int j = 0; j < tmpName.Count; j++) {
                    string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j].Substring(1);
                    wName += tmpWord + " ";
                }
                //wName += "("+wType+")";
                //wNameList new list containing wName
                List<string> wNameList = new List<string>();
                wNameList.Add(wName);
                wNameList.Add("(" + wType + ")");

                //draw solid rectangle centered on waterCenter with size of waterMaxRecSize and color of Black (DEBUG)
                //g.FillRectangle(new SolidBrush(Color.Black), waterCenter.Item1 - waterMaxRecSize.Item1 / 2, waterCenter.Item2 - waterMaxRecSize.Item2 / 2, waterMaxRecSize.Item1, waterMaxRecSize.Item2);

                //scale font2 to fit inside waterMaxRecSize
                int fontSize = 200; //minimum font size for name
                Font font2 = new Font(privateFontCollection.Families[0], fontSize);

                //check pixel size of font2
                SizeF size2 = g.MeasureString(wNameList[0], font2);
                //if size2 is smaller than waterMaxRecSize then increase font size to fit
                while (size2.Width < waterMaxRecSize.Item1 && size2.Height < (int)(waterMaxRecSize.Item2 * 0.8)) {
                    fontSize++;
                    font2 = new Font(privateFontCollection.Families[0], fontSize);
                    size2 = g.MeasureString(name, font2);
                }

                //check if single line wName would be bigger
                string wName2 = wNameList[0] + " " + wNameList[1];
                int fontSize2 = 200; //minimum font size for name
                Font font3 = new Font(privateFontCollection.Families[0], fontSize2);
                SizeF size3 = g.MeasureString(wName2, font3);
                while (size3.Width < waterMaxRecSize.Item1 && size3.Height < (int)(waterMaxRecSize.Item2 * 1.3)) {
                    fontSize2++;
                    font3 = new Font(privateFontCollection.Families[0], fontSize2);
                    size3 = g.MeasureString(wName2, font3);
                }

                //if single line wName would be bigger then use 2 lines
                if (fontSize > fontSize2) {

                    //draw all names in wNameList to rgoName image and move them down by Xpx each time
                    int y = waterCenter.Item2 - (int)(size2.Height * 0.15);
                    foreach (string s in wNameList) {
                        g.DrawString(s, font2, new SolidBrush(resColor), new Point(waterCenter.Item1, y), stringFormat);

                        //border outline
                        GraphicsPath p = new GraphicsPath();
                        p.AddString(
                            s,             // text to draw
                            privateFontCollection.Families[0],  // or any other font family
                            (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                            g.DpiY * font2.Size / 72,       // em size
                            new Point(waterCenter.Item1, y),              // location where to draw text
                            stringFormat);          // set options here (e.g. center alignment)
                        Pen p1 = new Pen(textColor, 4);
                        g.DrawPath(p1, p);

                        y += (int)(size2.Height * 0.5);
                    }
                }
                else {
                    int y = waterCenter.Item2 + (int)(size3.Height * 0.1);

                    g.DrawString(wName2, font3, new SolidBrush(resColor), new Point(waterCenter.Item1, y), stringFormat);

                    //border outline
                    GraphicsPath p = new GraphicsPath();
                    p.AddString(
                        wName2,             // text to draw
                        privateFontCollection.Families[0],  // or any other font family
                        (int)FontStyle.Regular,      // font style (bold, italic, etc.)
                        g.DpiY * font3.Size / 72,       // em size
                        new Point(waterCenter.Item1, y),              // location where to draw text
                        stringFormat);          // set options here (e.g. center alignment)
                    Pen p1 = new Pen(textColor, 4);
                    g.DrawPath(p1, p);

                }



                rgoMap.Save(localDir + "/_Output/RGOs/" + name.Replace("bg_", "") + ".png");
                rgoMap.Dispose();
            }

            return (waterCenter, waterMaxRecSize);

        }

        //set RGO Colors
        List<string> setRGOColors(List<Region> regionList) {
            List<string> rgoList = new List<string>();

            List<string> ignoreList = new List<string>();
            ignoreList.Add("bg_monuments");
            ignoreList.Add("bg_skyscraper");

            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (Resource res in s.resoures) {
                        if (s.center == (0, 0)) {
                            s.getCenter2(true);
                        }
                        ColorList(res);
                        if (!rgoList.Contains(res.name) && !ignoreList.Contains(res.name)) {
                            rgoList.Add(res.name);
                        }
                        else if (ignoreList.Contains(res.name)) {
                            Console.WriteLine("\t\tIgnoring " + res.name + " in " + s.name);
                        }
                    }
                }
            }
            return rgoList;
        }

        //RGO Colors
        void ColorList(Resource res) {
            //if rgoColors is empty then use default colors
            if (rgoColors.Count == 0) {
                if (res.name.Contains("gold") || res.name.Contains("sulfur")) {
                    res.color = Color.Gold;
                    res.textColor = Color.DarkBlue;
                }
                else if (res.name.Contains("farms") || res.name.Contains("banana")) {
                    res.color = Color.Yellow;
                    res.textColor = Color.Brown;
                }
                else if (res.name.Contains("oil_") || res.name.Contains("coal_")) {
                    res.color = Color.FromArgb(255, 37, 37, 37);
                    res.textColor = Color.Red;
                }
                else if (res.name.Contains("coffee_") || res.name.Contains("ranches")) {
                    res.color = Color.SaddleBrown;
                    res.textColor = Color.LimeGreen;
                }
                else if (res.name.Contains("cotton") || res.name.Contains("sugar")) {
                    res.color = Color.FromArgb(255, 85, 188, 187);
                    res.textColor = Color.DarkViolet;
                }
                else if (res.name.Contains("dye_") || res.name.Contains("silk_")) {
                    res.color = Color.DarkViolet;
                    res.textColor = Color.FromArgb(255, 85, 188, 187);
                }
                else if (res.name.Contains("logging") || res.name.Contains("rubber")) {
                    res.color = Color.BurlyWood;
                    res.textColor = Color.DarkGreen;
                }
                else if (res.name.Contains("plantation")) {
                    res.color = Color.Green;
                    res.textColor = Color.Purple;
                }
                else if (res.name.Contains("copper")) {
                    res.color = Color.Orange;
                    res.textColor = Color.DarkGreen;
                }
                else if (res.name.Contains("gemstone")) {
                    res.color = Color.DarkCyan;
                    res.textColor = Color.Red;
                }
                else if (res.name.Contains("mining") || res.name.Contains("tin_")) {
                    res.color = Color.SlateGray;
                    res.textColor = Color.Brown;
                }
                else if (res.name.Contains("fish") || res.name.Contains("whal")) {
                    res.color = Color.DarkCyan;
                    res.textColor = Color.FromArgb(255, 0, 0, 64);
                }
            }
            else {
                bool exitLoop = false;
                //interate throu rgoColors and set colors if found
                for (int i = 0; i < rgoColors.Count; i++) {
                    for (int j = 0; j < rgoColors[i].rgoNames.Count; j++) {
                        if (res.name.Contains(rgoColors[i].rgoNames[j])) {
                            res.color = rgoColors[i].hColor;
                            res.textColor = rgoColors[i].tColor;
                            exitLoop = true;
                            break;

                        }
                    }
                    if (exitLoop) break;
                }

            }
        }

        //merge maps
        void mergeMaps() {
            //if Output/BlankMap/ does not exist create it
            if (!Directory.Exists(localDir + "/_Output/BlankMap/")) {
                Directory.CreateDirectory(localDir + "/_Output/BlankMap/");
            }

            Bitmap waterColor = new Bitmap(localDir + "/_Output/ColorMap/water_map.png");
            Bitmap regionColor = new Bitmap(localDir + "/_Output/ColorMap/region_colors.png");
            Bitmap regionBorder = new Bitmap(localDir + "/_Output/BorderFrame/region_border.png");

            //merge 3 maps together into new image
            for (int i = 0; i < regionColor.Height; i++) {
                for (int j = 0; j < regionColor.Width; j++) {
                    if (waterColor.GetPixel(j, i).A != 0) {
                        regionColor.SetPixel(j, i, waterColor.GetPixel(j, i));
                    }
                    if (regionBorder.GetPixel(j, i).A != 0) {
                        regionColor.SetPixel(j, i, regionBorder.GetPixel(j, i));
                    }
                }
            }
            regionColor.Save(localDir + "/_Output/Region_Map.png");
            regionColor.Dispose();
            regionBorder.Dispose();

            Console.WriteLine("Merged Region Map\t" + sw.Elapsed);

            Bitmap stateColor = new Bitmap(localDir + "/_Output/ColorMap/state_colors.png");
            Bitmap stateBorder = new Bitmap(localDir + "/_Output/BorderFrame/state_border.png");

            //merge 3 maps together into new image
            for (int i = 0; i < stateColor.Height; i++) {
                for (int j = 0; j < stateColor.Width; j++) {
                    if (waterColor.GetPixel(j, i).A != 0) {
                        stateColor.SetPixel(j, i, waterColor.GetPixel(j, i));
                    }
                    if (stateBorder.GetPixel(j, i).A != 0) {
                        stateColor.SetPixel(j, i, stateBorder.GetPixel(j, i));
                    }
                }
            }
            stateColor.Save(localDir + "/_Output/State_Map.png");
            stateColor.Dispose();

            Console.WriteLine("Merged State Map\t" + sw.Elapsed);

            Bitmap blankProv = new Bitmap(waterColor.Width, waterColor.Height);
            Bitmap bp = new Bitmap(localDir + "/_Output/BorderFrame/prov_border.png");
            Graphics g = Graphics.FromImage(blankProv);
            g.Clear(Color.White);
            g.DrawImage(waterColor, Point.Empty);
            g.DrawImage(bp, Point.Empty);
            blankProv.Save(localDir + "/_Output/BlankMap/Province_Blank.png");
            blankProv.Dispose();
            bp.Dispose();
            Console.WriteLine("Merged Blank Province Map\t" + sw.Elapsed);

            Bitmap blankState = new Bitmap(waterColor.Width, waterColor.Height);
            Bitmap bs = new Bitmap(localDir + "/_Output/BorderFrame/state_border.png");
            g = Graphics.FromImage(blankState);
            g.Clear(Color.White);
            g.DrawImage(waterColor, Point.Empty);
            g.DrawImage(bs, Point.Empty);
            blankState.Save(localDir + "/_Output/BlankMap/State_Blank.png");
            blankState.Dispose();
            bs.Dispose();
            Console.WriteLine("Merged Blank State Map\t" + sw.Elapsed);

            Bitmap blankRegion = new Bitmap(waterColor.Width, waterColor.Height);
            Bitmap br = new Bitmap(localDir + "/_Output/BorderFrame/region_border.png");
            g = Graphics.FromImage(blankRegion);
            g.Clear(Color.White);
            g.DrawImage(waterColor, Point.Empty);
            g.DrawImage(br, Point.Empty);
            blankRegion.Save(localDir + "/_Output/BlankMap/Region_Blank.png");
            blankRegion.Dispose();
            br.Dispose();
            g.Dispose();
            Console.WriteLine("Merged Blank Region Map\t" + sw.Elapsed);
        }

        //write names on merged maps
        void namedMapes(List<Region> regionList) {
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for region names


            //region map
            Bitmap regionMap = new Bitmap(localDir + "/_Output/Region_Map.png");
            Graphics g = Graphics.FromImage(regionMap);


            for (int i = 0; i < regionList.Count; i++) {
                if (regionList[i].color != Color.FromArgb(0, 0, 0, 0)) {    //no ocean/sea names

                    regionList[i].getCenter2();

                    List<string> tmpName = regionList[i].name.Replace("region_", "").Split("_").ToList();
                    List<string> wName = new List<string>();
                    for (int j = 0; j < tmpName.Count; j++) {
                        string tmpWord = tmpName[j][0].ToString().ToUpper() + tmpName[j].Substring(1).ToLower();
                        wName.Add(tmpWord);
                    }

                    //if region maxRecSize width is atleast 2.5x the height merge wName into one line
                    if (regionList[i].maxRecSize.Item1 >= regionList[i].maxRecSize.Item2 * 2) {
                        string tmp = "";
                        for (int j = 0; j < wName.Count; j++) {
                            tmp += wName[j] + " ";
                        }
                        tmp = tmp.Trim();
                        wName.Clear();
                        wName.Add(tmp);
                    }

                    int numberFontSize = 25; //minimum font size for region name
                    Font font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                    //check pixel size of font1
                    //longest name in wName
                    string longestName = "";
                    for (int j = 0; j < wName.Count; j++) {
                        if (wName[j].Length > longestName.Length) {
                            longestName = wName[j];
                        }
                    }
                    double vertBias = 1.2;
                    if (wName.Count > 1) {
                        vertBias = 1.0;
                    }

                    bool fontTooSmall = false;
                    SizeF size1 = g.MeasureString(longestName, font1);
                    //if size1 is smaller than region maxRecSize then increase font size to fit
                    while (size1.Width < regionList[i].maxRecSize.Item1 * 1.2 && size1.Height < regionList[i].maxRecSize.Item2 * wName.Count * vertBias) {
                        numberFontSize++;
                        font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                        size1 = g.MeasureString(longestName, font1);
                    }

                    //for each word in wName draw it on the map and move down by size1.Height/2
                    int y = 0;
                    if (wName.Count > 1) {
                        y = regionList[i].center.Item2 - (int)(size1.Height * 0.35);
                    }
                    else {
                        y = regionList[i].center.Item2;
                    }

                    //if all 3 colors are less than 100, make text white
                    Color textColor = Color.Black;

                    if (rgbToYIQ(regionList[i].color) < 90) {
                        textColor = Color.White;
                    }

                    for (int j = 0; j < wName.Count; j++) {
                        g.DrawString(wName[j], font1, new SolidBrush(textColor), new Point(regionList[i].center.Item1, y), stringFormat);
                        y += (int)(size1.Height * 0.6);
                    }


                }
            }

            //save regionMap as png
            regionMap.Save(localDir + "/_Output/Region_Map_Names.png");



            //state map
            Bitmap stateMap = new Bitmap(localDir + "/_Output/State_Map.png");


            g = Graphics.FromImage(stateMap);


            for (int i = 0; i < regionList.Count; i++) {
                for (int j = 0; j < regionList[i].states.Count; j++) {
                    regionList[i].states[j].getCenter2();
                    if (regionList[i].color != Color.FromArgb(0, 0, 0, 0)) {    //no ocean/sea names
                        List<string> tmpName = regionList[i].states[j].name.Replace("STATE_", "").Split("_").ToList();
                        List<string> wName = new List<string>();
                        int wLength = -1;
                        for (int k = 0; k < tmpName.Count; k++) {
                            string tmpWord = tmpName[k][0].ToString().ToUpper() + tmpName[k].Substring(1).ToLower();
                            wName.Add(tmpWord);
                            wLength += tmpWord.Length;
                            wLength++;
                        }

                        //if more than 2 words in wName, merge into one line
                        if (wName.Count > 2) {
                            string tmp = "";
                            for (int k = 0; k < wName.Count; k++) {
                                tmp += wName[k] + " ";
                            }
                            tmp = tmp.Trim();
                            wName.Clear();
                            wName.Add(tmp);
                            //split wName[0] into 2 lines on the space that is closest to the middle
                            int spaceIndex = (int)(wName[0].Length * 0.4);
                            int tmpSpaceIndex = spaceIndex;
                            while (wName[0][tmpSpaceIndex] != ' ') {
                                tmpSpaceIndex++;
                            }
                            if (tmpSpaceIndex - spaceIndex < spaceIndex) {
                                spaceIndex = tmpSpaceIndex;
                            }
                            tmpSpaceIndex = spaceIndex;
                            while (wName[0][tmpSpaceIndex] != ' ') {
                                tmpSpaceIndex--;
                            }
                            if (spaceIndex - tmpSpaceIndex < spaceIndex) {
                                spaceIndex = tmpSpaceIndex;
                            }
                            string tmp1 = wName[0].Substring(0, spaceIndex);
                            string tmp2 = wName[0].Substring(spaceIndex + 1);
                            wName.Clear();
                            wName.Add(tmp1);
                            wName.Add(tmp2);

                            Console.WriteLine(wName[0] + " " + wName[0].Length + "\t" + wName[1] + " " + wName[1].Length);
                        }


                        //else if region maxRecSize width is atleast 2.1x the height merge wName into one line
                        else if ((regionList[i].states[j].maxRecSize.Item1 >= regionList[i].states[j].maxRecSize.Item2 * 2.1 || wLength < 8) && wName.Count > 1) {
                            string tmp = "";
                            for (int k = 0; k < wName.Count; k++) {
                                tmp += wName[k] + " ";
                            }
                            tmp = tmp.Trim();
                            wName.Clear();
                            wName.Add(tmp);

                            Console.WriteLine(wName[0] + " " + wName[0].Length);
                        }

                        //check pixel size of font1
                        //longest name in wName
                        string longestName = "";
                        for (int k = 0; k < wName.Count; k++) {
                            if (wName[k].Length > longestName.Length) {
                                longestName = wName[k];
                            }
                        }

                        int numberFontSize = 7; //minimum font size for state name
                        Font font2 = new Font("Verdna", numberFontSize);

                        SizeF size1 = g.MeasureString(longestName, font2);
                        double vertBias = 1.2;
                        if (wName.Count > 1) {
                            vertBias = 1.0;
                        }
                        bool fontTooSmall = false;
                        //if size1 is smaller than region maxRecSize then increase font size to fit
                        while (size1.Width < regionList[i].states[j].maxRecSize.Item1 && size1.Height < regionList[i].states[j].maxRecSize.Item2 * wName.Count * vertBias) {
                            numberFontSize++;
                            font2 = new Font("Verdna", numberFontSize);
                            size1 = g.MeasureString(longestName, font2);
                        }

                        int y = 0;
                        if (wName.Count > 2) {
                            y = regionList[i].states[j].center.Item2 - (int)(size1.Height * 3 / 4);
                        }
                        else if (wName.Count > 1) {
                            y = regionList[i].states[j].center.Item2 - (int)(size1.Height * 3 / 8);
                        }
                        else {
                            y = regionList[i].states[j].center.Item2;
                        }

                        //if all 3 colors are less than 100, make text white
                        Color textColor = Color.DarkBlue;

                        //check color of state with rgbToYIQ if less than 128 make text white
                        if (rgbToYIQ(regionList[i].states[j].color) < 90) {
                            textColor = Color.White;
                        }


                        for (int k = 0; k < wName.Count; k++) {
                            g.DrawString(wName[k], font2, new SolidBrush(textColor), new Point(regionList[i].states[j].center.Item1, y), stringFormat);
                            y += (int)(size1.Height * 0.7);
                        }
                    }
                }
            }

            //save stateMap as png
            stateMap.Save(localDir + "/_Output/State_Map_Names.png");
        }

        //how dark is the color
        float rgbToYIQ(Color c) {
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
        }
        
        //debug draw rectange around each region and state
        void debugDrawRectangle(List<Region> regionList, (int, int) waterRecCenter, (int, int) waterRecSize) {
            //if Output/Debug/ does not exist create it
            if (!Directory.Exists(localDir + "/_Output/Debug/")) {
                Directory.CreateDirectory(localDir + "/_Output/Debug/");
            }

            //creat a new blank region map
            Bitmap regionMap = new Bitmap(localDir + "/_Output/BlankMap/Region_Blank.png");
            Graphics g = Graphics.FromImage(regionMap);

            for (int i = 0; i < regionList.Count; i++) {
                //fill a solid rectangle of size maxRecSize and color regionList[i].color centered on regionList[i].center
                g.FillRectangle(new SolidBrush(regionList[i].color), regionList[i].center.Item1 - regionList[i].maxRecSize.Item1 / 2, regionList[i].center.Item2 - regionList[i].maxRecSize.Item2 / 2, regionList[i].maxRecSize.Item1, regionList[i].maxRecSize.Item2);

            }
            //fill a solid rectangle of size for water
            g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - waterRecSize.Item1 / 2, waterRecCenter.Item2 - waterRecSize.Item2 / 2, waterRecSize.Item1, waterRecSize.Item2);


            //save regionMap as png
            regionMap.Save(localDir + "/_Output/Debug/Region_Rectangles.png");
            regionMap.Dispose();

            //state map
            Bitmap stateMap = new Bitmap(localDir + "/_Output/BlankMap/State_Blank.png");
            g = Graphics.FromImage(stateMap);

            for (int i = 0; i < regionList.Count; i++) {
                for (int j = 0; j < regionList[i].states.Count; j++) {
                    //fill a solid rectangle of size maxRecSize and color regionList[i].color centered on regionList[i].state[j].center
                    g.FillRectangle(new SolidBrush(regionList[i].states[j].color), regionList[i].states[j].center.Item1 - regionList[i].states[j].maxRecSize.Item1 / 2, regionList[i].states[j].center.Item2 - regionList[i].states[j].maxRecSize.Item2 / 2, regionList[i].states[j].maxRecSize.Item1, regionList[i].states[j].maxRecSize.Item2);
                }
            }
            //fill a solid rectangle of size for water
            g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - waterRecSize.Item1 / 2, waterRecCenter.Item2 - waterRecSize.Item2 / 2, waterRecSize.Item1, waterRecSize.Item2);

            //save stateMap as png
            stateMap.Save(localDir + "/_Output/Debug/State_Rectangles.png");
            stateMap.Dispose();


            //state square
            Bitmap stateSquareMap = new Bitmap(localDir + "/_Output/BlankMap/State_Blank.png");
            g = Graphics.FromImage(stateSquareMap);
            for (int i = 0; i < regionList.Count; i++) {
                for (int j = 0; j < regionList[i].states.Count; j++) {
                    regionList[i].states[j].getCenter2(true);
                    //fill a solid rectangle of size maxRecSize and color regionList[i].color centered on regionList[i].state[j].center
                    g.FillRectangle(new SolidBrush(regionList[i].states[j].color), regionList[i].states[j].center.Item1 - regionList[i].states[j].maxRecSize.Item1 / 2, regionList[i].states[j].center.Item2 - regionList[i].states[j].maxRecSize.Item2 / 2, regionList[i].states[j].maxRecSize.Item1, regionList[i].states[j].maxRecSize.Item2);
                }
            }
            //fill a solid rectangle of size for water
            g.FillRectangle(new SolidBrush(Color.Black), waterRecCenter.Item1 - waterRecSize.Item1 / 2, waterRecCenter.Item2 - waterRecSize.Item2 / 2, waterRecSize.Item1, waterRecSize.Item2);

            //save stateMap as png
            stateSquareMap.Save(localDir + "/_Output/Debug/State_Square.png");
            stateSquareMap.Dispose();

        }

        void drawHubs(List<Region> regionList, Image image) {
            //create a new blank image of size image
            Bitmap hubMap = new Bitmap(image.Width, image.Height);

            //hub name color pairs
            Dictionary<string, Color> hubColor = new Dictionary<string, Color>();
            hubColor.Add("city", Color.Purple);
            hubColor.Add("port", Color.DarkCyan);
            hubColor.Add("mine", Color.Red);
            hubColor.Add("farm", Color.Yellow);
            hubColor.Add("wood", Color.DarkGreen);

            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    //for each prov in s.provDict
                    foreach (Province p in s.provDict.Values) {
                        //if p.hubname is in hubColor set all coords in p.coordList to hubColor[p.hubname]
                        if (hubColor.ContainsKey(p.hubName)) {
                            foreach ((int, int) coord in p.coordList) {
                                hubMap.SetPixel(coord.Item1, coord.Item2, hubColor[p.hubName]);
                            }
                        }
                    }
                }
            }

            hubMap.Save(localDir + "/_Output/ColorMap/hub_map.png");
        }

        void drawImapssiblePrime(List<Region> regionList, Image image) {
            //create a new blank image of size image
            Bitmap impossiblePrimeMap = new Bitmap(image.Width, image.Height);
            
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    //for each prov in s.provDict
                    foreach (Province p in s.provDict.Values) {
                        if (p.isImpassible) {
                            Color c = Color.Gray;
                            if(p.isLake || p.isSea) {
                                c = Color.Blue;
                            }
                            foreach ((int, int) coord in p.coordList) {
                                impossiblePrimeMap.SetPixel(coord.Item1, coord.Item2, c);
                            }
                        }
                        else if (p.isPrimeLand) {
                            Color c = Color.Green;
                            foreach ((int, int) coord in p.coordList) {
                                impossiblePrimeMap.SetPixel(coord.Item1, coord.Item2, c);
                            }
                        }

                    }
                }
            }

            impossiblePrimeMap.Save(localDir + "/_Output/ColorMap/impossible_prime_map.png");
        }

        Dictionary<string, Nation> parseNations(List<Region> regionList) {
            //dictionary of nation name and nation
            Dictionary<string, Nation> nationDict = new Dictionary<string, Nation>();

            //dictionary of color and prov from region.state.prov
            Dictionary<Color, Province> colorProv = new Dictionary<Color, Province>();
            //dictionary of state name and state from region.state
            Dictionary<string, State> stateDict = new Dictionary<string, State>();

            //for each region in regionList
            foreach (Region r in regionList) {
                //for each state in region.state
                foreach (State s in r.states) {
                    //add state to stateDict
                    stateDict.Add(s.name, s);
                }
            }


            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (Province p in s.provDict.Values) {
                        colorProv.Add(p.color, p);
                    }
                }
            }

            //read files in _Input/country_definitions
            string[] files = Directory.GetFiles(localDir + "/_Input/common/country_definitions");
            
            //read all lines in each file
            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                Nation n = new Nation();
                foreach (string line in lines) {
                    if (line.StartsWith("#") || line.Trim() == "") {
                        continue;
                    }

                    string l1 = line.Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("=", " = ").Replace("\"", "").Split("#")[0].Trim();

                    if (indent == 0) {
                        if (l1.Contains("=")) {
                            n = new Nation(l1.Split('=')[0].Trim());
                            nationDict.Add(n.name, n);
                        }
                    }
                    if (indent == 1) {
                        if (l1.StartsWith("color")) {
                            List<double> rgbValues = new List<double>();
                            string[] e = l1.Split('=')[1].Trim().Split(' ');
                            foreach (string s in e) {
                                //try to parse s as double
                                if (double.TryParse(s, out double d)) {
                                    //if d is between 0 and 1.01 then multiply it by 255
                                    if (d >= 0 && d <= 1.01) {
                                        d *= 255;
                                    }
                                    //if d is outside of 0 and 255 then set it to 0 or 255
                                    if (d < 0) {
                                        d = 0;
                                    }
                                    if (d > 255) {
                                        d = 255;
                                    }

                                    rgbValues.Add(d);
                                }
                            }
                            //if rgbValues has less than 3 values, add 128 till it has 3
                            while (rgbValues.Count < 3) {
                                rgbValues.Add(128);
                            }
                            if (l1.Contains("hsv360")) {
                                n.color = ColorFromHSV360(rgbValues[0], rgbValues[1], rgbValues[2]);
                            }
                            else if (l1.Contains("hvs")){
                                n.color = ColorFromHSV(rgbValues[0], rgbValues[1], rgbValues[2]);
                            }
                            else {
                                //set n.color to rgbValues
                                n.color = Color.FromArgb((int)rgbValues[0], (int)rgbValues[1], (int)rgbValues[2]);
                            }
                        }
                        //country_type
                        if (l1.StartsWith("country_type")) {
                            n.type = l1.Split('=')[1].Trim();
                        }
                        //tier
                        if (l1.StartsWith("tier")) {
                            n.tier = l1.Split('=')[1].Trim();
                        }
                        //cultures
                        if (l1.StartsWith("cultures")) {
                            n.cultures = l1.Replace("{", "").Replace("}", "").Split('=')[1].Trim().Split(' ').ToList();
                        }
                        //capital
                        if (l1.StartsWith("capital")) {
                            string capital = l1.Split('=')[1].Trim();
                            //find the state in stateDict that has the capital as its name
                            n.capital = stateDict[capital];                            
                        }
                    }


                    //if { or } is in l1, increment or decrement indent
                    if (l1.Contains("{")) {
                        indent++;
                    }
                    if (l1.Contains("}")) {
                        indent--;
                    }
                }
                

            }

            //read files in _Input/history/states
            files = Directory.GetFiles(localDir + "/_Input/common/history/states");

            //if files is not empty
            if (files.Count() == 0) {
                Console.WriteLine("No country definitions found");
                return null;
            }

            //read all lines in each file
            foreach (string file in files) {
                string[] lines = File.ReadAllLines(file);
                int indent = 0;
                Nation n = new Nation();
                State s = new State();
                bool createStateFound = false;
                int stateIndent = -1;
                bool stateProvsFound = false;
                int stateProvsIndent = -1;
                foreach (string line in lines) {
                    if (line.StartsWith("#") || line == "") {
                        continue;
                    }

                    string l1 = line.Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("=", " = ").Replace("\"", "").Split("#")[0].Trim();

                    if (indent == 1) {
                        if (l1.StartsWith("s:")) {
                            string stateName = l1.Split('=')[0].Split("s:")[1].Trim();
                            if (stateDict.ContainsKey(stateName)) {
                                s = stateDict[stateName];
                            }
                        }
                    }
                    if (indent == 2) {
                        //add_homeland
                        if (l1.StartsWith("add_homeland")) {
                            s.homeLandList.Add(l1.Split('=')[1].Trim());
                        }
                        //create_state
                        if (l1.StartsWith("create_state")) {
                            createStateFound = true;
                            stateIndent = indent;
                        }
                        //add_claim
                        if (l1.StartsWith("add_claim")) {
                            string statetag = l1.Split('=')[1].Trim();
                            if (nationDict.ContainsKey(statetag)) {
                                nationDict[statetag].claimList.Add(s);
                            }
                        }
                    }

                    if (createStateFound) {
                        //country
                        if (l1.StartsWith("country")) {
                            string tag = l1.Split("c:")[1].Trim();
                            if (nationDict.ContainsKey(tag)) {
                                n = nationDict[tag];
                            }
                        }
                        //owned_provinces
                        if (l1.StartsWith("owned_provinces")) {
                            stateProvsFound = true;
                            stateProvsIndent = indent;

                        }
                        //state_type
                        if (l1.StartsWith("state_type")) {
                            n.type = l1.Split('=')[1].Trim();
                        }

                    }
                    
                    if (stateProvsFound) {
                        string[] provs = l1.Replace("{", "").Replace("}", "").Replace("x", "#").Trim().Split();

                        if (l1.Contains('=')) {
                            provs = l1.Replace("{", "").Replace("}", "").Replace("x", "#").Split('=')[1].Trim().Split();
                        }
                        foreach (string p in provs) {
                            try {
                                //create new color using p as a hex string
                                Color c = ColorTranslator.FromHtml(p);
                                //if colorProv contains c as a key
                                if (colorProv.ContainsKey(c)) {
                                    //add colorProv[c] to n.provDict
                                    n.provDict.Add(c, colorProv[c]);
                                }
                            }
                            catch (Exception e) {
                                /*
                                Console.WriteLine(n.name + "\t" + p);
                                Console.WriteLine(e.Message);
                                */
                            }
                        }
                    }



                    //if { or } is in l1, increment or decrement indent
                    if (l1.Contains("{")) {
                        indent++;
                    }
                    if (l1.Contains("}")) {
                        indent--;
                        if (indent == stateIndent) {
                            createStateFound = false;
                        }
                        if (indent == stateProvsIndent) {
                            stateProvsFound = false;
                        }

                    }
                }
            }

            return nationDict;
        }

        void drawNationsMap(Dictionary<string, Nation> nationDict, string fileName, bool drawDecentralized) {
            Console.WriteLine("Drawing "+ fileName);

            //if the output folder doesn't exist, create it
            if (!Directory.Exists(localDir + "/_Output/National")) {
                Directory.CreateDirectory(localDir + "/_Output/National");
            }

            //create a new bitmap with the same size as the image
            Bitmap bitmap = new Bitmap(localDir+"/_Output/ColorMap/water_map.png");

            //for each nation in nationDict
            foreach (Nation n in nationDict.Values) {
                //draw decentralised nations?
                if (n.type == "decentralized" && !drawDecentralized) {
                    continue;
                }
                //for each province in n.provDict
                foreach (Province p in n.provDict.Values) {
                    //for each pixel in p set the pixel in bitmap to n.color
                    foreach ((int X, int Y) point in p.coordList) {
                        bitmap.SetPixel(point.X, point.Y, n.color);
                    }
                }
            }

            //save bitmap to _Output/nations.png
            bitmap.Save(localDir + "/_Output/National/" + fileName + ".png");

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(localDir + "/_Input/ParadoxVictorian-Condensed.otf"); //font for nation names

            Graphics g = Graphics.FromImage(bitmap);

            //add name to bitmap for each nation
            foreach (Nation n in nationDict.Values) {

                //draw decentralised nations?
                if (n.type == "decentralized" && !drawDecentralized) {
                    continue;
                }

                Color textColor = Color.Black;

                if (rgbToYIQ(n.color) < 90) {
                    textColor = Color.White;
                }
                
                //get the center of the nation
                n.getCenter2();

                string text = n.name.ToLower();

                int numberFontSize = 8; //minimum font size for nation name
                Font font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                //check pixel size of font1
                SizeF textSize = g.MeasureString(text, font1);

                //if size1 is smaller than state maxRecSize then increase font size to fit
                while (textSize.Width < n.maxRecSize.Item1 && textSize.Height < n.maxRecSize.Item2) {
                    numberFontSize++;
                    font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                    textSize = g.MeasureString(text, font1);
                }
                numberFontSize = (int)(numberFontSize * 1.3);


                int recFontSize = numberFontSize;

                //check if square getCenter2 would give a larger font size
                n.getCenter2(true);
                numberFontSize = 8; //minimum font size for nation name
                font1 = new Font(privateFontCollection.Families[0], numberFontSize);

                //check pixel size of font1
                textSize = g.MeasureString(text, font1);

                //if size1 is smaller than state maxRecSize then increase font size to fit
                while (textSize.Width < n.maxRecSize.Item1 && textSize.Height < n.maxRecSize.Item2) {
                    numberFontSize++;
                    font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                    textSize = g.MeasureString(text, font1);
                }
                numberFontSize = (int)(numberFontSize * 1.3);

                if (recFontSize > numberFontSize) {
                    //Console.WriteLine(n.name + " would be larger as a rectange by " + (recFontSize - numberFontSize) + " size");
                    n.getCenter2();
                    numberFontSize = recFontSize;                    
                }


                font1 = new Font(privateFontCollection.Families[0], numberFontSize);
                //draw the name on bitmap
                //drawText(bitmap, text, center.X, center.Y, Color.White);
                g.DrawString(text, font1, new SolidBrush(textColor), new Point(n.center.Item1, n.center.Item2), stringFormat);
            }

            bitmap.Save(localDir + "/_Output/National/" + fileName+"_tags.png");

        }

        Color ColorFromHSV(double v1, double v2, double v3) {
            //convert hsv to rgb
            double r, g, b;
            if (v3 == 0) {
                r = g = b = 0;
            }
            else {
                if (v2 == -1) v2 = 1;
                int i = (int)Math.Floor(v1 * 6);
                double f = v1 * 6 - i;
                double p = v3 * (1 - v2);
                double q = v3 * (1 - f * v2);
                double t = v3 * (1 - (1 - f) * v2);
                switch (i % 6) {
                    case 0: r = v3; g = t; b = p; break;
                    case 1: r = q; g = v3; b = p; break;
                    case 2: r = p; g = v3; b = t; break;
                    case 3: r = p; g = q; b = v3; break;
                    case 4: r = t; g = p; b = v3; break;
                    case 5: r = v3; g = p; b = q; break;
                    default: r = g = b = v3; break;
                }
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        Color ColorFromHSV360(double v1, double v2, double v3) {
            //converts hsv360 to rgb
            return ColorFromHSV(v1 / 360, v2 / 100, v3 / 100);
        }

        void parseTerrain(List<Region> regionList) {
            //prov dictionary for matching prov name to prov object
            Dictionary<Color, Province> provDict = new Dictionary<Color, Province>();

            //for each region in regionList
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (Province p in s.provDict.Values) {
                        //add province to provDict
                        provDict.Add(p.color, p);
                    }
                }
            }
            
            //read province_terrains.txt
            string[] lines = File.ReadAllLines(localDir + "/_Input/map_data/province_terrains.txt");

            int count = 1;
            //for each line in lines
            foreach (string line in lines) {
                if (line.StartsWith("#") || line.Trim() == "") {
                    continue;
                }
                if (line.Contains("=")) {
                    string[] l1 = line.Replace("\"", "").Trim().Split("=");

                    //match province to provDict on l1[0] to name
                    Color c = ColorTranslator.FromHtml(l1[0].Replace("x", "#"));
                    if (provDict.ContainsKey(c)) {
                        //set prov.terrain to l1[1]
                        provDict[c].terrain = l1[1];
                        provDict[c].intearnlID = count;
                    }
                    else {
                        //Console.WriteLine("Error: " + c + " not found in provDict");
                    }
                }
                count++;

            }



        }

        void parseSave(List<Region> regionList, Dictionary<string, Nation> nationDict, string filePath) {
            //prov dictionary for matching prov intearnlID to prov object
            Dictionary<int, Province> provDict = new Dictionary<int, Province>();

            //for each region in regionList
            foreach (Region r in regionList) {
                foreach (State s in r.states) {
                    foreach (Province p in s.provDict.Values) {
                        //check if p.intearnlID is -1
                        if (p.intearnlID == -1) {
                            Console.WriteLine("Error: " + p.name + " has no intearnlID");
                            continue;
                        }
                        //add province to provDict
                        provDict.Add(p.intearnlID, p);
                    }
                }
            }

            //sort on intearnlID
            provDict = provDict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            //reset all provDict in nationDict
            foreach (Nation nat in nationDict.Values) {
                nat.provDict = new Dictionary<Color, Province>();
                nat.maxRecSize = (0, 0);
                nat.center = (0, 0);
            }
            
            //read all lines in save file from filePath
            string[] lines = File.ReadAllLines(filePath);

            int indintation = -2;
            Nation n = null;
            int potentialID = -1;
            bool foundSeaNodes = false;
            bool civilWarFound = false;
            //for each line in lines
            foreach (string line in lines) {
                if (line.Trim().StartsWith("#") || line.Trim() == "") {
                    continue;
                }
                string l1 = line.Replace("{", " { ").Replace("}", " }").Trim().Split("#")[0];

                if (l1.Contains("sea_nodes")) {
                    foundSeaNodes = true;
                }

                if (indintation == 0) {
                    if (l1.Contains("= {")) {
                        string l2 = l1.Split("=")[0].Trim();
                        // if l1.Split("=")[0] is int
                        if (int.TryParse(l2, out potentialID)) {
                            //Console.WriteLine(potentialID);
                        }
                    }
                }
                if (indintation == 1) {
                    if (l1.StartsWith("definition=")) {                    
                        string tag = l1.Split("=")[1].Replace("\"", "").Trim();
                        if (tag.Length < 8) {
                            //Console.WriteLine(indintation);
                            if (nationDict.ContainsKey(tag) && !civilWarFound) {
                                n = nationDict[tag];
                                n.interalID = potentialID;
                                //Console.WriteLine(n);
                            }
                            else if(civilWarFound) {
                                try {
                                    //supprot for dynaimc tags
                                    n = new Nation(tag + "_cw");
                                    n.interalID = potentialID;
                                    //assigne n a random color
                                    n.color = ColorFromHSV360(rand.Next(0, 360), 100, 100);
                                    nationDict.Add(tag + "_cw", n);
                                    Console.WriteLine(n);
                                    civilWarFound = false;
                                }
                                catch{
                                    //Console.WriteLine("Error: " + tag + " is a duplicate Civil War tag?");
                                }
                            }
                        }

                    }
                    else if (l1.StartsWith("map_color=rgb")) {
                        List<int> rgbValues = new List<int>();
                        //try to parss l1 words to int, if they are add them to rgbValues
                        foreach (string s in l1.Split("=")[1].Replace("(", "").Replace(")", "").Replace("rgb", "").Split(",")) {
                            if (int.TryParse(s, out int i)) {
                                rgbValues.Add(i);
                            }
                        }
                        //if rgbValues has 3 values
                        if (rgbValues.Count == 3) {
                            //set n.color to rgbValues
                            n.color = Color.FromArgb(rgbValues[0], rgbValues[1], rgbValues[2]);
                        }

                    }
                    else if (l1.StartsWith("civil_war=yes")) {
                        civilWarFound = true;
                    }
                    else if (l1.StartsWith("country=")) {
                        //try parse int on l1[1]
                        if (int.TryParse(l1.Split("=")[1].Trim(), out potentialID)) {
                            try {
                                //set n to nationDict where n.interalID == potentialID
                                n = nationDict.Values.Where(x => x.interalID == potentialID).First();
                                //Console.WriteLine(n);

                            }
                            catch {
                                //Console.WriteLine("Error: " + potentialID + " not found in nationDict");
                            }
                        }
                    }
                }
                if (indintation > 1 && !foundSeaNodes) {
                    if (l1.StartsWith("provinces=")) {
                        //Console.WriteLine(l1);
                        //if nation with interalID == potentialID in nationDict, set n to it

                        try {
                            //find nation with interalID == potentialID
                            n = nationDict.Values.Where(x => x.interalID == potentialID).First();

                            //check if substring after = has less than 3 characters
                            if (l1.Split("=")[1].Trim().Length < 3) {
                                continue;
                            }

                            //split on = and trim
                            string[] l2 = l1.Split("=")[1].Replace("{", "").Replace("}", "").Trim().Split();

                            //try parse int on l2
                            List<int> idList = new List<int>();
                            foreach (string s in l2) {
                                int id;
                                if (int.TryParse(s, out id)) {
                                    idList.Add(id);
                                }
                            }

                            //int in idList are in pairs of 2 where they reperent a range starting and the first number and ending at the first+second number
                            //for each int in idList
                            for (int i = 0; i < idList.Count; i += 2) {
                                //Console.WriteLine(n + "\t\t\t" + idList[i] + "\t" + idList[i + 1]);
                                //for each int in ranged
                                for (int j = idList[i]; j < idList[i] + idList[i + 1]+1; j++) {
                                    //check if provDict contains j
                                    if (provDict.ContainsKey(j)) {
                                        //add provDict[j] to n.provDict
                                        n.provDict.Add(provDict[j].color, provDict[j]);
                                    }
                                    else {
                                        //Console.WriteLine("Error: " + j + " not found in provDict");
                                    }
                                }
                            }
                            //Console.WriteLine(n);
                        }
                        catch { }
                    }
                }
                


                if (l1.Contains("{") || l1.Contains("}")) {
                    string[] w = l1.Split();
                    foreach (string s in w) {
                        if (s == "{") {
                            indintation++;
                        }
                        else if (s == "}") {
                            indintation--;
                        }
                    }

                }
            }
            /*
            //print out the first 10 nations in nationDict and their provDict size
            int count = 0;
            foreach (Nation nat in nationDict.Values) {
                Console.WriteLine(nat);
                count++;
                if (count == 20) {
                    break;
                }
            }
            */
        }
    
        void writeRGOs(List<Region> regionList) {
            //check if _Output/TextFiles folder exists
            if (!Directory.Exists(localDir + "/_Output/TextFiles/")) {
                Directory.CreateDirectory(localDir + "/_Output/TextFiles/");
            }

            //dictionary of resource type and resource name
            Dictionary<string, List<string>> resDict = new Dictionary<string, List<string>>();

            //go through each region in regionList
            foreach (Region r in regionList) {
                foreach(State s in r.states) {
                    //go through each resource in s.resDict
                    foreach (Resource res in s.resoures) {
                        //if resDict does not contain res.type
                        if (!resDict.ContainsKey(res.type)) {
                            //add res.type to resDict
                            resDict.Add(res.type, new List<string>());
                        }
                        //if resDict[res.type] does not contain res.name
                        if (!resDict[res.type].Contains(res.name)) {
                            //add res.name to resDict[res.type]
                            resDict[res.type].Add(res.name);
                        }
                    }
                }
            }

            //create a new csv file for each RGOs
            StreamWriter stwr = new StreamWriter(localDir + "/_Output/TextFiles/RGOs.csv");

            //headder
            stwr.Write("Region;State;");
            foreach (string resType in resDict.Keys) {
                //resource
                if (resType == "resource") {
                    foreach (string resName in resDict[resType]) {
                        stwr.Write(resName.Replace("bg_", "") + ";");
                    }
                }
            }
            foreach (string resType in resDict.Keys) {
                //discoverable
                if (resType == "discoverable") {
                    foreach (string resName in resDict[resType]) {
                        stwr.Write("Known "+resName.Replace("bg_","") + ";");
                        stwr.Write("Discoverable " + resName.Replace("bg_", "") + ";");
                    }
                }
            }
            stwr.Write("Arable Land;Agricultureal RGOs\n");


            foreach (Region r in regionList) {
                if (r.states.Count < 2) continue; //don't write naval states/regions

                //stwr.Write(r.name.Replace("region_","") + "\n");
                foreach(State s in r.states) {
                    stwr.Write(r.name.Replace("region_", "") +";" + s.name.ToLower().Replace("state_","") + ";");

                    //resource
                    foreach (string resType in resDict.Keys) {
                        if (resType == "resource") {
                            foreach (string resName in resDict[resType]) {
                                //if res with resName is in s.resoures lsit
                                if (s.resoures.Where(x => x.name == resName).Count() > 0) {
                                    //write res.amount
                                    stwr.Write(s.resoures.Where(x => x.name == resName).First().knownAmmount + ";");
                                }
                                else {
                                    //write 0
                                    stwr.Write("0;");
                                }
                            }
                        }
                    }
                    //discoverable
                    foreach (string resType in resDict.Keys) {
                        if (resType == "discoverable") {
                            foreach (string resName in resDict[resType]) {
                                //if res with resName is in s.resoures lsit
                                if (s.resoures.Where(x => x.name == resName).Count() > 0) {
                                    //write res.amount
                                    stwr.Write(s.resoures.Where(x => x.name == resName).First().knownAmmount + ";");
                                    stwr.Write(s.resoures.Where(x => x.name == resName).First().discoverableAmmount + ";");
                                }
                                else {
                                    //write 0
                                    stwr.Write("0;");
                                    stwr.Write("0;");
                                }
                            }
                        }
                    }

                    //arable
                    stwr.Write(s.arableLand + ";");
                    foreach (Resource res in s.resoures) {
                        if (res.type == "agriculture") {
                            stwr.Write(res.name.Replace("bg_", "") + " ");
                        }
                    }



                    stwr.Write("\n");
                }
            }



            stwr.Close();

        }

        void inputCFG(){
            //get cfg file in _Input folder
            string[] cfgFiles = Directory.GetFiles(localDir + "/_Input/", "*.cfg");


            string[] lines = File.ReadAllLines(cfgFiles[0]);

            bool colorFound = false;
            //go through each line in input.cfg
            foreach (string line in lines) {
                string l1 = line.Split("#")[0].Trim();
                if (l1 == "") continue;

                //doDrawRGOs
                if (l1.StartsWith("DrawRGOs")) {
                    doDrawRGOs = bool.Parse(l1.Split("=")[1].Trim());
                }
                //doDrawStartingNations
                if (l1.StartsWith("DrawStartingNations")) {
                    doDrawStartingNations = bool.Parse(l1.Split("=")[1].Trim());
                }
                //doDrawSaves
                if (l1.StartsWith("DrawSaves")) {
                    doDrawSaves = bool.Parse(l1.Split("=")[1].Trim());
                }
                //doDrawDecentralized
                if (l1.StartsWith("DrawDecentralized")) {
                    doDrawDecentralized = bool.Parse(l1.Split("=")[1].Trim());
                }

                //Color
                if (l1.StartsWith("Color")) {
                    colorFound = true;
                }

                if (colorFound && l1.Contains("(")) {

                    List<string> buildingSubstring = l1.Split('=')[0].Split(',').ToList();
                    //trim list
                    for (int i = 0; i < buildingSubstring.Count; i++) {
                        buildingSubstring[i] = buildingSubstring[i].Trim();
                    }

                    //split l1 on ( ) , and space
                    List<string> colorSubstring = l1.Split(new char[] { '(', ')', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    List<int> color = new List<int>();
                    foreach (string s in colorSubstring) {
                        //try parse int
                        if (int.TryParse(s, out int i)) {
                            //if i is out of range 0-255 set to 0 or 255
                            if (i < 0) i = 0;
                            if (i > 255) i = 255;
                            color.Add(i);
                        }
                    }
                    //if color is not 6 long add 128 to the end
                    if (color.Count < 6) {
                        color.Add(128);
                    }


                    Color hColor = Color.FromArgb(color[0], color[1], color[2]);
                    Color tColor = Color.FromArgb(color[3], color[4], color[5]);


                    rgoColors.Add((buildingSubstring, hColor, tColor));
                }
            }
        }

        void debugStateProv(List<Region> regionList) {
            //group regionList by number of states in each region
            var stateCount = regionList.GroupBy(x => x.states.Count).OrderBy(x => x.Key);

            List<string> lines = new List<string>();

            lines.Add("Region;State;Provinces\n");
            //print out number of states in each region
            foreach (var s in stateCount) {
                lines.Add(s.Key + ";" + s.Count()+";\n");
                Console.WriteLine(s.Key + " states: " + s.Count());
            }
            Console.WriteLine();
            lines.Add("\n");

            //group states by number of provinces
            var provCount = regionList.SelectMany(x => x.states).GroupBy(x => x.provDict.Count).OrderBy(x => x.Key);

            //print out number of provinces in each state
            foreach (var p in provCount) {
                lines.Add(";"+p.Key + ";" + p.Count() + "\n");
                Console.WriteLine(p.Key + " provinces: " + p.Count());
                
            }

            

            //write to file
            StreamWriter stwr = new StreamWriter(localDir + "/_Output/Debug/StateProv.csv");

            //write lines
            foreach (string line in lines) {
                stwr.Write(line);
            }
            stwr.Close();

        }
    }
}