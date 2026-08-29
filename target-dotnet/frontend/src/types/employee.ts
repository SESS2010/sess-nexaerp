// Mirrors SESS.NexaERP.Application.Employees contracts (camelCase over the wire).

export interface EmployeeSummary {
  id: string
  employeeCode: string
  employeeName: string
  employeeType: string
  grade: string
  department: string
  skillCategory: string
  jobDesignation: string
  status: string
  loginEnabled: boolean
  approvalStatus: string
}

export interface EmployeeRoleSummary {
  id: string
  roleCode: string
  roleName: string
  effectiveFrom: string
  effectiveTo: string | null
  approvalStatus: string
  remarks: string
}

export interface EmployeeDetail {
  id: string
  employeeCode: string
  employeeName: string
  originalImportedName: string
  employeeType: string
  grade: string
  department: string
  skillCategories: string[]
  jobDesignation: string
  status: string
  dateOfJoining: string | null
  officialEmail: string | null
  mobileNumber: string | null
  loginEnabled: boolean
  approvalStatus: string
  roles: EmployeeRoleSummary[]
}

export interface EmployeeHistorySummary {
  id: string
  action: string
  fromStatus: string
  toStatus: string
  remarks: string
  createdAt: string
  createdBy: string
}

export interface MasterLookupItem {
  code: string
  name: string
}

export interface EmployeeMasterLookups {
  departments: MasterLookupItem[]
  skills: MasterLookupItem[]
  designations: MasterLookupItem[]
}

export interface CreateEmployeeRequest {
  employeeCode: string
  employeeName: string
  employeeType: string
  grade: string
  departmentCode: string
  skillCode: string
  designationCode: string
  dateOfJoining: string | null
  officialEmail: string | null
  mobileNumber: string | null
  remarks: string
}

export interface UpdateEmployeeRequest {
  employeeName: string
  employeeType: string
  grade: string
  departmentCode: string
  skillCode: string
  designationCode: string
  dateOfJoining: string | null
  officialEmail: string | null
  mobileNumber: string | null
  reason: string
}
