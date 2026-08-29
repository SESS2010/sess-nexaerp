// Mirrors SESS.NexaERP.Application.Employees contracts.
// The API enforces PascalCase JSON property names globally (ApiJsonContract).

export interface EmployeeSummary {
  Id: string
  EmployeeCode: string
  EmployeeName: string
  EmployeeType: string
  Grade: string
  Department: string
  SkillCategory: string
  JobDesignation: string
  Status: string
  LoginEnabled: boolean
  ApprovalStatus: string
}

export interface EmployeeRoleSummary {
  Id: string
  RoleCode: string
  RoleName: string
  EffectiveFrom: string
  EffectiveTo: string | null
  ApprovalStatus: string
  Remarks: string
}

export interface EmployeeDetail {
  Id: string
  EmployeeCode: string
  EmployeeName: string
  OriginalImportedName: string
  EmployeeType: string
  Grade: string
  Department: string
  SkillCategories: string[]
  JobDesignation: string
  Status: string
  DateOfJoining: string | null
  OfficialEmail: string | null
  MobileNumber: string | null
  LoginEnabled: boolean
  ApprovalStatus: string
  Roles: EmployeeRoleSummary[]
}

export interface EmployeeHistorySummary {
  Id: string
  Action: string
  FromStatus: string
  ToStatus: string
  Remarks: string
  CreatedAt: string
  CreatedBy: string
}

export interface MasterLookupItem {
  Code: string
  Name: string
}

export interface EmployeeMasterLookups {
  Departments: MasterLookupItem[]
  Skills: MasterLookupItem[]
  Designations: MasterLookupItem[]
}

export interface CreateEmployeeRequest {
  EmployeeCode: string
  EmployeeName: string
  EmployeeType: string
  Grade: string
  DepartmentCode: string
  SkillCode: string
  DesignationCode: string
  DateOfJoining: string | null
  OfficialEmail: string | null
  MobileNumber: string | null
  Remarks: string
}

export interface UpdateEmployeeRequest {
  EmployeeName: string
  EmployeeType: string
  Grade: string
  DepartmentCode: string
  SkillCode: string
  DesignationCode: string
  DateOfJoining: string | null
  OfficialEmail: string | null
  MobileNumber: string | null
  Reason: string
}
