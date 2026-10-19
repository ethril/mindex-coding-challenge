# Mindex Coding Challenge
I’ve added verbose comments in this repo to document my thoughts and decisions. The original prompt was moved to [PROMPT.md](PROMPT.md).

## API Endpoints
The API listens on `localhost:8080`. Every `{id}` in a URL is the employee's `employeeId`.

### Employee (Provided)

- **CREATE** an employee
  - `POST` `/api/employee`
  - Payload: `Employee`
  - Response: `Employee`

  ```http
  POST http://localhost:8080/api/employee
  Content-Type: application/json

  {
    "firstName": "Wolfgang",
    "lastName": "Mozart",
    "position": "Composer",
    "department": "Music"
  }
  ```
- **READ** an employee
  - `GET` `/api/employee/{id}`
  - Response: `Employee`

  ```http
  GET http://localhost:8080/api/employee/16a596ae-edd3-4847-99fe-c4518e82c86f
  ```
- **UPDATE** an employee
  - `PUT` `/api/employee/{id}`
  - Payload: `Employee`
  - Response: `Employee`

  ```http
  PUT http://localhost:8080/api/employee/b7839309-3348-463b-a7e3-5de1c168beb3
  Content-Type: application/json

  {
    "employeeId": "b7839309-3348-463b-a7e3-5de1c168beb3",
    "firstName": "Paul",
    "lastName": "McCartney",
    "position": "Developer II",
    "department": "Engineering"
  }
  ```

### Employee Reporting Structure (Task 1)

- **READ** an employee's reporting structure
  - `GET` `/api/reportingStructure/{id}`
  - Response: `ReportingStructure`

  ```http
  GET http://localhost:8080/api/reportingStructure/16a596ae-edd3-4847-99fe-c4518e82c86f
  ```

### Employee Compensation (Task 2)

- **CREATE** a compensation for an employee
  - `POST` `/api/compensation`
  - Payload: `Compensation`
  - Response: `Compensation`

  ```http
  POST http://localhost:8080/api/compensation
  Content-Type: application/json

  {
    "employee": { "employeeId": "16a596ae-edd3-4847-99fe-c4518e82c86f" },
    "salary": 125000,
    "effectiveDate": "2026-01-01"
  }
  ```
- **READ** the compensation in effect today
  - `GET` `/api/compensation/{id}`
  - Response: `Compensation`

  ```http
  GET http://localhost:8080/api/compensation/16a596ae-edd3-4847-99fe-c4518e82c86f
  ```
- **READ** the compensation in effect on a specific day
  - `GET` `/api/compensation/{id}?asOf=YYYY-MM-DD`
  - Response: `Compensation`

  ```http
  GET http://localhost:8080/api/compensation/16a596ae-edd3-4847-99fe-c4518e82c86f?asOf=2026-06-30
  ```