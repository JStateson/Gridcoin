﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;


namespace BTHistoryReader
{
    public partial class BTHistory : Form
    {
        public BTHistory()
        {
            InitializeComponent();
            InitLookupTable();
        }


        static string str_PathToHistory;
        static string[] LinesHistory;
        static string ReqVer = "1.79";
        static string ReqID = "BoincTasks History";
        static double dAvgCreditPerUnit;
        static int iPadSize;
        static int[] iSortIndex;

        const int LKUP_NOT_FOUND = -1;      // cannot find project- forgot it or new one
        const int LKUP_TOBE_IGNORED = -2;   // do not use this project
        const int LKUP_INVALID_LINE = -3;   // line in history is invalid 


        public string CurrentSystem;   // computer name 
        public List<cProjectInfo> ThisProjectInfo;

        // pad right side with spaces to fill
        public static string Rpadto(string strIn, int cnt)
        {
            int i = cnt - strIn.Length;
            if (i < 0) return strIn.Substring(0, cnt);
            return strIn + "                              ".Substring(0, i);
        }

        // pad left side with spaces to fill
        public static string Lpadto(string strIn, int cnt)
        {
            int i = cnt - strIn.Length;
            if (i < 0) return strIn.Substring(0, cnt);
            return "                              ".Substring(0, i) + strIn;
        }

        // output  is 1, 2 or 3 digit number formatted with space  after (example for "3") shown
        //  eg:  xxxxx  1 xxxx
        //       xxxxx123 xxx
        public static string fmtLineNumber(string strVal)
        {
            return Lpadto(strVal, iPadSize) + " ";
        }



        public int LookupApp(string strIn, int iLoc)
        {
            int n = 0;
            foreach (cAppName AppName in KnownProjApps[iLoc].KnownApps)
            {
                if (strIn == AppName.Name)
                {
                    return n;
                }
                n++;
            }
            return -1;
        }

        public int LookupProject(string strName)
        {
            int i = 0;
            foreach (cKnownProjApps kpa in KnownProjApps)
            {
                if (strName == kpa.ProjName) return i;
                i++;
            }
            return LKUP_NOT_FOUND;
        }

        private static string fmtHMS(long seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"hh\:mm\:ss");
        }

        // this is our lookup table
        public  List<cKnownProjApps> KnownProjApps;

        // return index to project name in table else an error code that is negative
        // return name of project were were trying to find
        // first non numeric non white character is start of project name: 43	SETI@home	SE
        // there is a numeric value followed by a tab.  Look for tab
        public int LookupProj(string strIn, ref string strFoundName)
        {
            int i = strIn.Length;
            int iIndex = 1 + strIn.IndexOf('\t');
            if (iIndex <= 0) return LKUP_INVALID_LINE; 
            strIn = strIn.Substring(iIndex);
            iIndex = 0;
            foreach (cKnownProjApps kpa in KnownProjApps)
            {
                int j = kpa.ProjName.Length;
                if (i < j) return LKUP_INVALID_LINE;  // cannot be in this line
                strFoundName = strIn.Substring(0, j);
                if (strFoundName == kpa.ProjName)
                {
                    if (kpa.bIgnore) return LKUP_TOBE_IGNORED;
                    return iIndex;
                }
                iIndex++;
            }
            return LKUP_NOT_FOUND;
        }

        // assume max of 32 projects and max of 4 apps
        private void InitLookupTable()
        {
            cKnownProjApps kpa;
            KnownProjApps = new List<cKnownProjApps>();

            kpa = new cKnownProjApps();
            kpa.AddName("Milkyway@Home");
            kpa.AddApp("Milkyway@home Separation");
            KnownProjApps.Add(kpa);

            kpa = new cKnownProjApps();
            kpa.AddName("SETI@home");
            kpa.AddApp("SETI@home v8");
            kpa.AddApp("AstroPulse v7");
            KnownProjApps.Add(kpa);

            kpa = new cKnownProjApps();
            kpa.AddName("collatz");
            kpa.AddApp("Collatz Sieve");
            KnownProjApps.Add(kpa);

            kpa = new cKnownProjApps();
            kpa.AddName("Amicable Numbers");
            kpa.AddApp("Amicable Numbers up to 10^20");
            KnownProjApps.Add(kpa);

            kpa = new cKnownProjApps();
            kpa.AddName("World Community Grid");
            kpa.AddApp("Mapping Cancer Markers");
            kpa.AddApp("FightAIDS@Home - Phase 1");
            kpa.AddApp("FightAIDS@Home - Phase 2");
            kpa.AddApp("OpenZika");
            kpa.AddApp("Microbiome Immunity Project");
            KnownProjApps.Add(kpa);

            kpa = new cKnownProjApps();
            kpa.AddName("GPUGRID");
            kpa.AddApp("Short runs (2-3 hours on fastest card)");
            kpa.AddApp("Long runs (8-12 hours on fastest card)");
            KnownProjApps.Add(kpa);

            // this project ignored
            kpa = new cKnownProjApps();
            kpa.AddName("WUProp@Home");
            KnownProjApps.Add(kpa);

            //lb_NumKnown.Text = "Known Projects: " + KnownProjApps.Count.ToString();
            
        }

        public void ClearPreviousHistory()
        {
            foreach (cKnownProjApps kpa in KnownProjApps)
            {
                kpa.EraseAppInfo();
                lb_SelWorkUnits.Items.Clear();
                cb_AppNames.Items.Clear();
                cb_AppNames.Text = "";
                cb_SelProj.Items.Clear();
                cb_SelProj.Text = "";
                tb_AvgCredit.Text = "0";
                tb_Info.Text = "";
                tb_Results.Text = "";
            }
        }

        private void btn_OpenHistory_Click(object sender, EventArgs e)
        {
            string str_WantedDirectory = "\\EFmer\\BoincTasks\\history";
            string str_LookHere = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ofd_history.DefaultExt = ".cvs?";
            str_PathToHistory = str_LookHere + str_WantedDirectory;
            tb_Results.Text = "";
            if (!Directory.Exists(str_PathToHistory))
            {
                tb_Info.Text = "Cannot find " + str_PathToHistory + "\r\n---Trying\r\n";
                // look where executable is located
                str_PathToHistory = Directory.GetCurrentDirectory();
                str_LookHere = str_PathToHistory + "\r\n";
                tb_Info.Text += str_LookHere;
            }
            ofd_history.InitialDirectory = str_PathToHistory;
            ofd_history.ShowDialog();
            lb_history_loc.Text = ofd_history.FileName;
            if(File.Exists(lb_history_loc.Text))
            {
                str_PathToHistory = lb_history_loc.Text;
                if (ValidateHistory() >= 0)
                {
                    ClearPreviousHistory();
                    CurrentSystem = LinesHistory[1];
                    BTHistory.ActiveForm.Text = CurrentSystem; ;   // this is name of the computer
                    ProcessHistoryFile();
                    FillSelectBoxes();
                }
                else
                {
                    tb_Info.Text += "problem with history file\r\n";
                }
            }
            else
            {
                tb_Info.Text += "file does not exist\r\n";
                str_PathToHistory = "";
            }
        }

        int ValidateHistory()
        {
            int iErr = 0;

            try
            {
                LinesHistory = File.ReadAllLines(str_PathToHistory);
            }
            catch (Exception e)
            {
                tb_Info.Text += (string)e.Data["MSG"] + "\r\n";
                return -1;
            }
            if (LinesHistory[0] == ReqVer && LinesHistory[2] == ReqID) return 0;
            else
            {
                tb_Info.Text += "cannot find " + ReqVer + " or " + ReqID + "\r\n";
                return -2;
            }
   

            return 0;
        }

        // iLoc is index to project table and we need list of apps to show
        void FillAppBox(int iLoc)
        {
            int i=0, n = 0;
            bool bAny = false;

            cb_AppNames.Items.Clear();
            foreach (cAppName appName in KnownProjApps[iLoc].KnownApps)
            {
                n = appName.nAppEntries;
                if(n > 0)
                {
                    bAny = true;
                    cb_AppNames.Items.Add(appName.Name + "  (" + n.ToString() + ")");
                }
                i++;
            }
            Debug.Assert(bAny);
            cb_AppNames.Text = cb_AppNames.Items[0].ToString();
            cb_AppNames.Tag = i;    // use tag to restore any edits to the combo box as I cant make it readonly
        }

        // get list of projects
        int ProcessHistoryFile()
        {
            int iLine = -4;  // if > 4 then 
            int RtnCode;
            bool bFound;
            string strProjOut = "";
            cKnownProjApps kpa;

            // find and identify any project in the file
            foreach (string s in LinesHistory)
            {
                iLine++;
                if (iLine < 1) continue;    // skip past header
                // possible sanity check here: iLine is 1 and first token of "s" is also 1
                RtnCode = LookupProj(s, ref strProjOut);
                //  may want to identify unknown projects
                //  any syntax errors skip the incomplete line and all that follow
                if (RtnCode < 0)
                {
                    if(RtnCode == LKUP_NOT_FOUND)
                    {
                        tb_Info.Text = "Cannot find project: " + strProjOut + " adding to database\r\n";
                        kpa = new cKnownProjApps();
                        kpa.AddUnkProj(strProjOut);
                        KnownProjApps.Add(kpa);
                        RtnCode = KnownProjApps.Count-1;  // put unknown project here
                    }
                    else continue;
                }
                // if the app is found then point to the line containing the app's info
                KnownProjApps[RtnCode].SymbolInsert(s, 3+iLine);  // first real data is in 5th line (0..4)
            }
            return 0;
        }




        // fill in the project selection combo box
        void FillSelectBoxes()
        {
            int n;
            string strProjName;

            foreach (cKnownProjApps kpa in KnownProjApps)
            {
                n = kpa.nAppsUsed;
                if (n > 0 && !kpa.bIgnore)
                {
                    cb_SelProj.Items.Add(kpa.ProjName);
                }
            }
            if (cb_SelProj.Items.Count == 0) return;
            strProjName = cb_SelProj.Items[0].ToString();
            n = LookupProject(strProjName);
            cb_SelProj.Text = strProjName;
            cb_SelProj.Tag = n;                     // tag select project box with current project#
            FillAppBox(n);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void cb_SelProj_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = cb_SelProj.SelectedIndex;
            cb_SelProj.Text = cb_SelProj.Items[i].ToString();
            i = LookupProject(cb_SelProj.Text);
            FillAppBox(i);
        }

        // standard bubble sort with exchange on index
        public void SortTimeIncreasing(int nSort)
        {
            iSortIndex = new int[nSort];
            int i, j, k;
            int j1, j2;
            string sTemp;
            k = nSort - 2;
            for (i = 0; i < nSort; i++)
                iSortIndex[i] = i;
            for(i=0; i < k; i++)
            {
                for(j = 0; j < k; j++)
                {
                    j1 = iSortIndex[j];
                    j2 = iSortIndex[j + 1];
                    if(ThisProjectInfo[j1].time_t_Completed > ThisProjectInfo[j2].time_t_Completed)
                    {
                        iSortIndex[j] = j2;
                        iSortIndex[j+1] = j1;
                    }
                }
            }
            for(i=0; i<nSort; i++)
            {
                j = iSortIndex[i];
                sTemp = ThisProjectInfo[j].strOutput;
                lb_SelWorkUnits.Items.Add(sTemp);
            }
        }


        /*
Project           0
Application       1
Version Number    2
Name              3
PlanClass         4
Elapsed Time Cpu  5
Elapsed Time Gpu  6
State             7
ExitStatus        8
Reported time     9
Completed time   10 
Use              11
Received         12
VMem             13
Mem              14
         */

        public void FillProjectInfo(cAppName AppName)
        {
            string[] strSymbols;
            string sTemp;
            System.DateTime dt_1970 = new System.DateTime(1970, 1, 1);
            System.DateTime dt_this;
            int j = 0;
            long n, nElapsedTime;

            foreach (int i in AppName.LineLoc)
            {
                strSymbols = LinesHistory[i].Split('\t');
                ThisProjectInfo[j].strLineNum = strSymbols[0];
                sTemp = strSymbols[11];                             // this is completed time in seconds based on 1970    
                n = Convert.ToInt64(sTemp);                         // want to convert to time stamp
                ThisProjectInfo[j].time_t_Completed = n;
                if (n <= 0)
                {
                    break;  // is 0 if not calculated yet
                }
                dt_this = dt_1970.AddSeconds(n);
                sTemp = fmtLineNumber(strSymbols[0]) + dt_this.ToString();
                ThisProjectInfo[j].strCompletedTime = sTemp;        // save in readable format
                nElapsedTime = Convert.ToInt64(strSymbols[6].ToString()); // this is elapsed time
                n -= nElapsedTime;                                  // get the correct start time as best as we can
                ThisProjectInfo[j].time_t_Started = n;              // needed to calculate throughput
                ThisProjectInfo[j].strElapsedTimeCpu = fmtHMS(nElapsedTime);
                ThisProjectInfo[j].strElapsedTimeGpu = fmtHMS(Convert.ToInt64(strSymbols[7].ToString()));
                sTemp += " " + ThisProjectInfo[j].strElapsedTimeCpu;
                //    "(" + ThisProjectInfo[j].strElapsedTimeGpu + ")";
                ThisProjectInfo[j].strOutput = sTemp;               // eventually put into our text box to allow selections
                j++;
            }
            SortTimeIncreasing(j);
        }

        private void btnFetchHistory_Click(object sender, EventArgs e)
        {
            int iProject, iApp, i;
            cAppName AppName;
            string strProjName;

            i = cb_SelProj.SelectedIndex;
            if(i < 0)   // invalid selection. restore original project name using "tag"
            {
                tb_Info.Text = "cannot find project: " + cb_SelProj.Text + " \r\n Restoreing";
                if (cb_SelProj.Tag == null) return;
                strProjName = KnownProjApps[(int)cb_SelProj.Tag].ProjName;
                cb_SelProj.Text = strProjName;
                return;
            }
            strProjName = cb_SelProj.Items[i].ToString();
            iProject = LookupProject(strProjName);
            Debug.Assert(iProject >= 0);
            iApp = cb_AppNames.SelectedIndex;
            if (iProject < 0 || iApp < 0)
                return;
            AppName = KnownProjApps[iProject].KnownApps[iApp];
            ThisProjectInfo = new List<cProjectInfo>(AppName.nAppEntries);
            lb_SelWorkUnits.Items.Clear();
            iPadSize = Convert.ToInt32( Math.Floor(Math.Log10(AppName.nAppEntries) + 1));  // want to know how many digits in row counter
            for (i = 0;i < AppName.nAppEntries;i++)
            {
                cProjectInfo cpi = new cProjectInfo();
                ThisProjectInfo.Add(cpi);
            }
            FillProjectInfo(AppName);
        }


        // the first number shown in the selection box is line number in the history file, not the index to the project info table
        private void btn_Filter_Click(object sender, EventArgs e)
        {
            long t_start, t_stop, t_diff;
            int i, j, k;
            int i1, i2; // used to access iSort..
            double dSeconds = 0;
            int nItems;
            double dUnitsPerSecond;
            int NumUnits = lb_SelWorkUnits.SelectedItems.Count;
            string sTemp, s1, s2;

            if (NumUnits != 2)
            {
                tb_Results.Text = "you must select exactly two items\r\n";
                return;
            }
            i = lb_SelWorkUnits.SelectedIndices[0]; // difference between this shows the selection
            j = lb_SelWorkUnits.SelectedIndices[1];

            sTemp = lb_SelWorkUnits.SelectedItems[0].ToString();
            //i1 = Convert.ToInt32(sTemp.Substring(0, iPadSize)) - 1;  // origin is 0 not 1
            s1 = sTemp.Substring(iPadSize + 1); // remove digits and space
            k = s1.IndexOf("M ");
            s1 = s1.Substring(0, k + 1);

            sTemp = lb_SelWorkUnits.SelectedItems[1].ToString();
            //i2 = Convert.ToInt32(sTemp.Substring(0, iPadSize)) - 1;
            s2 = sTemp.Substring(iPadSize + 1); // remove digits and space
            k = s2.IndexOf("M ");
            s2 = s2.Substring(0, k + 1);

            t_start = ThisProjectInfo[i].time_t_Started;
            t_stop  = ThisProjectInfo[j].time_t_Completed;
            tb_Results.Text  = "Start time " + s1 + "\r\n";
            tb_Results.Text += "Stop  time " + s2 + "\r\n";
            t_diff = t_stop - t_start;  // seconds
            dSeconds = (double)t_diff;
            nItems = 1 + j - i;
            dUnitsPerSecond = nItems / dSeconds;
            tb_Results.Text += "Elapsed seconds: " + dSeconds.ToString("###,##0\r\n");
            tb_Results.Text += "Number Work Units: " + nItems + "\r\n";
            tb_Results.Text += "Units per second: " + dUnitsPerSecond.ToString("###,##0.0000\r\n");
            dAvgCreditPerUnit = Convert.ToDouble(tb_AvgCredit.Text);
            tb_Results.Text += "Credits/sec (system): " + (dUnitsPerSecond * dAvgCreditPerUnit).ToString("#,##0.00\r\n");
            nItems = Convert.ToInt32( tbNDevices.Text);
            tb_Results.Text += "Credits/sec (one device): " + (dUnitsPerSecond * dAvgCreditPerUnit / nItems).ToString("##0.00\r\n");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lb_SelWorkUnits.SelectedIndices.Clear();
        }

        private void cb_AppNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            string strTemp = cb_AppNames.Text;

        }


      //frm2.ShowDialog(); //shows form as a modal dialog
      //frm2.Show();    //shows form as a non modal dialog
      //frm2.Dispose();   
        private void btnShowProjectTree_Click(object sender, EventArgs e)
        {
            InfoForm MyInfo = new InfoForm(this);
            MyInfo.ShowDialog();
        }

        private void btnContinunity_Click(object sender, EventArgs e)
        {
            int NumUnits = lb_SelWorkUnits.SelectedItems.Count;;
            int i, j, k, nItems;
            double a,b,c, MaxDiff = 0.0;
            int iLocMaxDiff=0;

            lb_LocMax.Text = "";
            lbTimeContinunity.Text = "";

            if (NumUnits != 2)
            {
                tb_Results.Text = "you must select exactly two items\r\n";
                return;
            }
            i = lb_SelWorkUnits.SelectedIndices[0]; // difference between this shows the selection
            j = lb_SelWorkUnits.SelectedIndices[1];
            nItems = 1 + j - i;
            for(k=0; k < nItems - 1; k++)
            {
                a = ThisProjectInfo[iSortIndex[i+k]].time_t_Completed;
                b = ThisProjectInfo[iSortIndex[i+k+1]].time_t_Completed;
                c = b - a;
                if(c > MaxDiff)
                {
                    MaxDiff = c;
                    iLocMaxDiff = k+i;
                }
            }
            MaxDiff /= 60.0;    // to minutes
            lbTimeContinunity.Text = "Most minutes between tasks: " + MaxDiff.ToString("###,##0.00") ;
            if(MaxDiff > 0.0)
            {
                string strLine = lb_SelWorkUnits.Items[iLocMaxDiff].ToString().TrimStart();
                i = strLine.IndexOf(' ');
                lb_LocMax.Text = "Near line# " + strLine.Substring(0, i);
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MyAbout myAbout = new MyAbout();
            myAbout.ShowDialog();
        }
    }
}