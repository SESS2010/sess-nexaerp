SESS SERVICE ENGINEER HUB – VERSION 2.6.0 – COMPLETE PACKAGE (23 Sep 2026)

1_App_Code      Code.gs, Index.html, appsscript.json  -> paste into Apps Script (every upgrade)
2_Backend_Excel SESS_Service_Engineer_Backend_V2_6_With_Legacy_Data.xlsx
                -> ONLY for a brand-new installation.
                -> Your app already runs on a Google Sheet: DO NOT upload this again (it would replace live data).
                   For an upgrade just run setup() – it adds all new columns/settings automatically.
3_Guides        COMPLETE_GUIDE_V2.6.pdf  (start here)
                PAGE_ACCESS.md (what each role sees), DATA_MODEL.md (359 columns, for NexaERP),
                CHANGE_HISTORY.md (all versions)

UPGRADE IN SHORT: paste the 3 code files -> delete old extra files -> run setup -> run installDailyBackup (once)
-> run systemHealthCheck (version 2.6.0) -> Deploy > Manage deployments > Edit > New version -> Ctrl+F5.
Keep this zip in Google Drive > SESS Service Hub > Packages.
