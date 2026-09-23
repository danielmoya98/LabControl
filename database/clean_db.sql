-- Limpieza completa de tablas para inicio en limpio de pruebas
TRUNCATE TABLE 
  "RegistrosConsumoEnergia", 
  "SesionesUso", 
  "BloquesHorarios", 
  "Computadoras", 
  "Aulas", 
  "Materias", 
  "Docentes", 
  "PeriodosAcademicos", 
  "Bloques", 
  "Sedes", 
  "AspNetUserRoles", 
  "AspNetUserClaims", 
  "AspNetUserLogins", 
  "AspNetUserTokens", 
  "AspNetUsers", 
  "AspNetRoleClaims", 
  "AspNetRoles" 
RESTART IDENTITY CASCADE;
