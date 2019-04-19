﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BTHistoryReader
{
    // fillowing struct is mostly filled in as history is parsed
    public class cProjectInfo
    {
        public string strLineNum;
        public string strProject;
        public string strApplication;
        public string strVersionNumber;
        public string strName;
        public string strPlanClass;
        public string strElapsedTimeCpu;    // actually this is the "elapsed time"
        public double dElapsedTime;
        public string strElapsedTimeGpu;    // actually this is the cpu time that is in parens ie: "00:02:45(00:00:17)"
        public double dElapsedCPU;  // this seems to really be CPU time
        public string strState;     // seems a '3' here is aborted or bad
        public bool bState; // if true then state is valid can can be counted in average & std computations
        public string strExitstatus;
        public string strReportedTime;
        public string strCompletedTime;
        public long time_t_Started;
        public long time_t_Completed;
        public long time_t_Diff_C_S;
        public string strUse;
        public string strReceived;
        public string strVMem;
        public string strMem;
        public string strOutput;
    }

    public class cAppName
    {
        public string Name;
        public cKnownProjApps ptrKPA;
        public string GetInfo
        {
            get { return ptrKPA.ProjName + "\\" + Name; }
        }
        public int NumberBadWorkUnits;
        public double AvgRunTime;
        public double StdRunTime;
        public void FormStats()
        {
            int nUsed = 0;
            AvgRunTime = 0.0;
            StdRunTime = 0.0;
            foreach(int i in LineLoc)
            {

            }
        }
        public List<int> LineLoc;
        public int nAppEntries
        {
            get { return LineLoc.Count; }
        }
        public void init()
        {
            LineLoc.Clear();
            AvgRunTime = 0.0;
            StdRunTime = 0.0;
        }
        public bool bIsUnknown;
    }

    // this structure is the "symbol table" used to lookup apps found in history
    // it may be missing apps as new ones are created by the projects
    public class cKnownProjApps
    {
        public string ProjName;
        public bool bIsUnknown;
        public bool bContainsUnknownApps;
        public int NumberBadWorkUnits;
        public List<cAppName> KnownApps;
        public cAppName FindApp(string strName)
        {
            foreach(cAppName AppName in KnownApps)
            {
                if (AppName.Name == strName)
                    return AppName;
            }
            Debug.Assert(false);
            return null;
        }
        private int CountActualEntries()
        {
            int n = 0;
            foreach (cAppName AppName in KnownApps)
            {
                if (AppName.nAppEntries > 0) n++;
            }
            return n;
        }
        public int nAppsDefined         // there are this many in the lookup table
        {
            get { return KnownApps.Count; }
        }
        public int nAppsUsed         // there are this many referenced in the history file
        {
            get { return CountActualEntries(); }
        }
        public bool bIgnore;    // wuprop project is ignored or any that have no apps
        public void AddName(string strIn)
        {
            ProjName = strIn;
            KnownApps = new List<cAppName>();
            bIgnore = true; // assume no apps for this project
            bContainsUnknownApps = false;   // assume apps are known
            bIsUnknown = false;
        }
        public void AddUnkProj(string strIn)
        {
            ProjName = strIn;
            KnownApps = new List<cAppName>();
            bIgnore = true; // assume no apps for this project
            bIsUnknown = true;
        }

        public void AddApp(string strIn)
        {
            cAppName AppName = new cAppName();
            AppName.ptrKPA = this;
            AppName.Name = strIn;
            AppName.LineLoc = new List<int>();
            KnownApps.Add(AppName);
            bIgnore = false;  
            bIsUnknown = false;
        }
        public cAppName AddUnkApp(string strIn)
        {
            cAppName AppName = new cAppName();
            AppName.Name = strIn;
            AppName.ptrKPA = this;
            AppName.LineLoc = new List<int>();
            KnownApps.Add(AppName);
            bIgnore = false;   
            AppName.bIsUnknown = true;
            bContainsUnknownApps = true;
            return AppName;
        }

        // look for known apps but if unknown found then insert it
        public cAppName SymbolInsert(string strAppName, int iLoc)
        {
            cAppName UnkAppName;
            foreach (cAppName AppName in KnownApps)
            {
                if (strAppName == AppName.Name)
                {
                    AppName.LineLoc.Add(iLoc);
                    AppName.bIsUnknown = false;
                    return AppName;    // was a known app or was in database
                }
            }
            UnkAppName = AddUnkApp(strAppName);
            UnkAppName.LineLoc.Add(iLoc);
            UnkAppName.bIsUnknown = true;
            return UnkAppName; // was an unknown app
        }

        // remove all traces of app results
        public void EraseAppInfo()
        {
            NumberBadWorkUnits = 0;
            foreach(cAppName AppName in KnownApps)
            {
                AppName.LineLoc.Clear();
                AppName.NumberBadWorkUnits = 0;
            }
        }
    }

}
