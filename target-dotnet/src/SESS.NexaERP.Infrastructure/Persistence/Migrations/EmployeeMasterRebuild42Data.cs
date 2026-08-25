namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class EmployeeMasterRebuild42Data
{
    internal sealed record RosterRow(
        string Code, string Name, string? OldCode, string[] MatchKeys, string Gender, string Qualification,
        string Dob, string Doj, string DesignationCode, string DesignationName, string PrimaryDepartment,
        string[] SecondaryDepartments, string? NewEmployeeId);

    internal sealed record LeaverRow(string Code, string Name, string[] MatchKeys);

    internal static readonly RosterRow[] Roster =
    [
        R("SESS-01","PARAMANANTHAM A","SESS-001",["APARAMANANTHAM","PARAMANANTHAMA"],"Male","-","1979-06-15","2010-01-01","TECHNICAL_DIRECTOR","Technical Director","MANAGEMENT",[]),
        R("SESS-02","ALAGUEASWARI P","SESS-002",["ALAGUEASWARI","PALAGUEASWARI","ALAGUEASWARIP"],"Female","-","1985-06-03","2010-01-01","MANAGING_DIRECTOR","Managing Director","ACCOUNTS",["MANAGEMENT"]),
        R("SESS-03","KRISHNAVENI","SESS-021",["KRISHNAVENI"],"Female","-","1980-02-20","2024-12-25","HOUSEKEEPING","Housekeeping","HR",[]),
        R("SESS-04","DINESH T","SESS-004",["TDINESH","DINESHT"],"Male","DIP","1989-05-07","2022-01-16","DGM_TECHNICAL_SUPPORT","DGM - Technical Support","SERVICE",["REFRIGERATION"]),
        R("SESS-05","SATHISHKUMAR M","SESS-003",["MSATHISHKUMAR","SATHISHKUMARM"],"Male","DEG","1992-12-12","2018-06-16","SR_REFRIGERATION_ENGINEER","Sr. Refrigeration Engineer","REFRIGERATION",["SERVICE"]),
        R("SESS-06","NANTHAKUMAR S","SESS-006",["SNANTHAKUMAR","NANTHAKUMARS"],"Male","DEG","2000-04-12","2022-02-01","ELECTRICAL_ENGINEER","Electrical Engineer","SERVICE",["ELECTRICAL","REFRIGERATION","FABRICATION","PLC_LABVIEW","AMC","CAMC"]),
        R("SESS-07","LALU","SESS-013",["LALU"],"Male","ITI","1995-04-01","2022-02-01","FABRICATOR","Fabricator","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-08","WASEEM S","SESS-005",["WASEEMS"],"Male","ITI","1988-06-01","2023-02-02","FABRICATOR","Fabricator","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-09","MANIKANDAN S","SESS-009",["MANIKANDANS"],"Male","DEG","2004-04-19","2024-01-02","SERVICE_TECHNICIAN","Service Technician","REFRIGERATION",["ELECTRICAL","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-10","RAJESH KUMAR V","SESS-010",["RAJESHKUMARV"],"Male","ITI","1997-11-14","2024-01-29","ELECTRICAL_ENGINEER","Electrical Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-11","YESWANTH KUMAR N","SESS-011",["YESWANTHKUMARN"],"Male","ITI","1998-09-28","2024-06-20","TECHNICIAN","Technician","SERVICE",["ELECTRICAL","REFRIGERATION","FABRICATION","PLC_LABVIEW","AMC","CAMC"]),
        R("SESS-12","SURANTHER P","SESS-008",["SURANTHERP"],"Male","DEG","1992-05-20","2024-07-05","SOFTWARE_DEVELOPER","Software Developer","IT",[]),
        N("SESS-13","PARAMESHWARAN S","Male","ITI","1966-04-04","2024-03-18","FABRICATION_INCHARGE","Fabrication Incharge","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"7cac9ed0-16fa-4e43-bfc8-65af3d696885"),
        R("SESS-14","ALFATHIMA PARVEEN A","SESS-007",["AALFATHIMAPARVEEN","ALFATHIMAPARVEENA"],"Female","DEG","2003-03-07","2022-12-02","ACCOUNTANT_MANAGER","Accountant Manager","ACCOUNTS",["STORES","PURCHASE"]),
        R("SESS-15","PRIYA E","SESS-012",["PRIYAE"],"Female","DEG","1989-01-29","2024-10-21","PURCHASE_INCHARGE","Purchase Incharge","PURCHASE",["STORES"]),
        R("SESS-16","KAMALI SRINIVASAN","SESS-014",["KAMALISRINIVASAN"],"Female","DEG","1996-06-03","2024-12-04","STORE_INCHARGE","Store Incharge","STORES",["PURCHASE"]),
        R("SESS-17","RANJITH E","SESS-015",["RANJITHE"],"Male","DIP","2001-07-28","2024-12-09","DESIGN_ENGINEER","Design Engineer","DESIGN",["QC"]),
        R("SESS-18","MOHD ASHIQ","SESS-017",["MOHDASHIQ"],"Male","DEG","2000-09-14","2024-12-19","ELECTRICAL_ENGINEER","Electrical Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-19","RANJITH R","SESS-019",["RANJITHR"],"Male","DEG","1999-04-27","2025-01-02","DESIGN_ENGINEER","Design Engineer","DESIGN",["QC"]),
        R("SESS-20","PRAKASAM B","SESS-024",["PRAKASAMB"],"Male","DIP","1976-01-03","2025-04-10","ELECTRICAL_ENGINEER","Electrical Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-21","RANJEETH B","SESS-020",["RANJEETHB"],"Male","DEG","1997-08-09","2025-04-10","HR_MANAGER","HR Manager","HR",[]),
        R("SESS-22","KARTHIKEYAN M.K","SESS-025",["KARTHIKEYANMK"],"Male","DEG","1992-06-05","2025-04-21","FABRICATOR","Fabricator","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-23","SRINIVASAN V","SESS-026",["SRINIVASANV"],"Male","ITI","1992-01-22","2025-04-30","FABRICATOR","Fabricator","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-24","VINAYAGAM P","SESS-035",["VINAYAGAM","VINAYAGAMP"],"Male","ITI","1971-06-03","2025-05-02","FABRICATOR","Fabricator","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-25","SARATH BABU K","SESS-023",["SARATHBABUK"],"Male","DEG","1993-08-30","2025-05-03","PRODUCTION_MANAGER","Production Manager","PRODUCTION",["SALES"]),
        R("SESS-26","SRINIVASAN C","SESS-029",["SRINIVASANC"],"Male","ITI","1979-03-29","2025-07-05","REFRIGERATION_ENGINEER","Refrigeration Engineer","AMC",["ELECTRICAL","REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","CAMC"]),
        R("SESS-27","MANIKANDAN S","SESS-030",["MANIKANDANSOKKALINGAM"],"Male","DEG","2004-04-19","2025-09-01","ELECTRICAL_ENGINEER","Electrical Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-28","VENKAT RAV","SESS-031",["VENKATRAVS","VENKATRAV"],"Male","DEG","2004-04-11","2025-10-06","JUNIOR_ACCOUNTANT","Junior Accountant","ACCOUNTS",[]),
        R("SESS-29","BLESSON PAUL","SESS-033",["BLESSONPAUL"],"Male","DEG","2003-05-16","2025-10-13","JUNIOR_ENGINEER","Junior Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        R("SESS-30","SYED IJAZUDDIN Z","SESS-038",["SYEDIJAZUDDINZ"],"Male","DEG","1994-05-07","2025-12-17","PLC_PROGRAMMER","PLC Programmer","PLC_LABVIEW",["ELECTRICAL","REFRIGERATION","FABRICATION","SERVICE","AMC","CAMC"]),
        R("SESS-31","MADHAN KUMAR J","SESS-034",["MADHANKUMARJ"],"Male","ITI","1992-05-10","2026-01-12","REFRIGERATION_TECHNICIAN","Refrigeration Technician","REFRIGERATION",["ELECTRICAL","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"]),
        N("SESS-32","ILAMPARUTHI D","Male","DEG","2001-12-02","2026-03-09","JR_SOFTWARE_DEVELOPER","JR. Software Developer","IT",[],"d54067d8-2b91-4311-9959-d8720a48a23b"),
        N("SESS-33","NARREN S","Male","DEG","1994-12-02","2026-04-06","PRODUCTION_QUALITY_INCHARGE","Production & Quality Incharge","QC",["ELECTRICAL","REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"8886f556-7322-47b9-bc62-baede7e3c074"),
        N("SESS-34","BHUVANESH M","Male","DEG","2005-09-05","2026-05-06","REFRIGERATION_TECHNICIAN","Refrigeration Technician","REFRIGERATION",["ELECTRICAL","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"51cc53ec-1fa5-4060-9a11-f20fa28a7723"),
        N("SESS-35","SUDALAI K","Male","DEG","1999-10-26","2026-05-07","STORE_EXECUTIVE","Store Executive","STORES",["PURCHASE"],"feedba1d-8a12-47db-b1a3-9a2d0307aa84"),
        N("SESS-36","MOHAMED ASICK","Male","DEG","2004-07-23","2026-05-07","ELECTRICAL_ENGINEER","Electrical Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"4dc6bc3e-4bed-42a4-a36a-827d113baf30"),
        N("SESS-37","BARATH KUMAR D.S","Male","DEG","1999-10-15","2026-05-11","ELECTRICAL_ENGINEER","Electrical Engineer","ELECTRICAL",["REFRIGERATION","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"b11e13b1-f3fd-469b-849e-bb2231ad071d"),
        N("SESS-38","PANBARASU G","Male","ITI","1992-05-01","2026-05-15","REFRIGERATION_ENGINEER","Refrigeration Engineer","REFRIGERATION",["ELECTRICAL","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"214dbe51-d27c-4165-b5e2-b6da387522aa"),
        N("SESS-39","SRINIVASAN R","Male","DEG","1982-02-24","2026-05-26","FABRICATOR","Fabricator","FABRICATION",["ELECTRICAL","REFRIGERATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"7903912a-88aa-45e2-8e0a-88fb2d6ff19f"),
        N("SESS-40","MAGESHWARI K","Male","DEG","2002-04-21","2026-06-08","JR_SOFTWARE_DEVELOPER","JR. Software Developer","IT",[],"6649dd67-580c-446c-88f0-913a707c26e8"),
        N("SESS-41","KARTHICK E","Male","DEG","1996-03-26","2026-06-10","SR_ACCOUNTANT","Sr. Accountant","STORES",["ACCOUNTS"],"3967580d-b0fc-4139-92cd-d356b83ee6c8"),
        N("SESS-42","PUSHPARAJ P","Male","ITI","1985-05-24","2026-06-10","REFRIGERATION_ENGINEER","Refrigeration Engineer","REFRIGERATION",["ELECTRICAL","FABRICATION","PLC_LABVIEW","SERVICE","AMC","CAMC"],"1b168ed3-f15d-4fb5-bf67-25afb529e561")
    ];

    internal static readonly LeaverRow[] Leavers =
    [
        L("SESS-016","KALIDOSS",["KALIDOSS"]),
        L("SESS-018","A. VINAYA SAGAR ARKATI",["AVINAYASAGARARKATI"]),
        L("SESS-022","KARTHICK.B",["KARTHICKB"]),
        L("SESS-027","SANJAY SARAVANAN",["SANJAYSARAVANAN"]),
        L("SESS-028","PRAVEEN KUMAR.M",["PRAVEENKUMARM"]),
        L("SESS-032","PRASANNA.G",["PRASANNAG"]),
        L("SESS-036","FRANCIS XAVIER",["FRANCISXAVIER"]),
        L("SESS-037","DEVANAND B",["DEVANANDB"]),
        L("SESS-039","THIRUNAVUKKARASU",["THIRUNAVUKKARASU"])
    ];

    internal static string RosterSql => string.Join(",\n", Roster.Select(x =>
        $"({Q(x.Code)},{Q(x.Name)},{NQ(x.OldCode)},{A(x.MatchKeys)},{Q(x.Gender)},{Q(x.Qualification)},DATE {Q(x.Dob)},DATE {Q(x.Doj)},{Q(x.DesignationCode)},{Q(x.DesignationName)},{Q(x.PrimaryDepartment)},{A(x.SecondaryDepartments)},{Uuid(x.NewEmployeeId)})"));

    internal static string LeaverSql => string.Join(",\n", Leavers.Select(x =>
        $"({Q(x.Code)},{Q(x.Name)},{A(x.MatchKeys)})"));

    internal static string ReverseCodeSql => string.Join(",\n", Roster.Where(x => x.OldCode is not null).Select(x =>
        $"({Q(x.Code)},{Q(x.OldCode!)})"));

    internal static string NewEmployeeUuidSql => string.Join(",", Roster.Where(x => x.NewEmployeeId is not null).Select(x => Q(x.NewEmployeeId!)));

    private static RosterRow R(string c,string n,string old,string[] keys,string g,string q,string dob,string doj,string dc,string dn,string pd,string[] sd) =>
        new(c,n,old,keys,g,q,dob,doj,dc,dn,pd,sd,null);

    private static RosterRow N(string c,string n,string g,string q,string dob,string doj,string dc,string dn,string pd,string[] sd,string id) =>
        new(c,n,null,[],g,q,dob,doj,dc,dn,pd,sd,id);

    private static LeaverRow L(string c,string n,string[] keys) => new(c,n,keys);
    private static string Q(string value) => "'" + value.Replace("'","''",StringComparison.Ordinal) + "'";
    private static string NQ(string? value) => value is null ? "NULL" : Q(value);
    private static string Uuid(string? value) => value is null ? "NULL" : Q(value) + "::uuid";
    private static string A(IEnumerable<string> values) => "ARRAY[" + string.Join(",",values.Select(Q)) + "]::text[]";
}
