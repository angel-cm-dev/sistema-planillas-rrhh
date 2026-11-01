IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Afps] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    [AportePct] decimal(7,4) NOT NULL,
    [PrimaSeguroPct] decimal(7,4) NOT NULL,
    [ComisionPct] decimal(7,4) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Afps] PRIMARY KEY ([Id])
);

CREATE TABLE [Areas] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Areas] PRIMARY KEY ([Id])
);

CREATE TABLE [Cargos] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Cargos] PRIMARY KEY ([Id])
);

CREATE TABLE [ParametrosPlanilla] (
    [Id] int NOT NULL IDENTITY,
    [Uit] decimal(12,2) NOT NULL,
    [Rmv] decimal(12,2) NOT NULL,
    [AsignacionFamiliarPct] decimal(7,4) NOT NULL,
    [OnpPct] decimal(7,4) NOT NULL,
    [EsSaludPct] decimal(7,4) NOT NULL,
    [HoraExtra25Pct] decimal(7,4) NOT NULL,
    [HoraExtra35Pct] decimal(7,4) NOT NULL,
    [TopePrimaAfp] decimal(12,2) NOT NULL,
    [HorasJornadaDiaria] decimal(4,2) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_ParametrosPlanilla] PRIMARY KEY ([Id])
);

CREATE TABLE [Planillas] (
    [Id] int NOT NULL IDENTITY,
    [Anio] int NOT NULL,
    [Mes] int NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    [FechaCierre] datetime2 NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Planillas] PRIMARY KEY ([Id])
);

CREATE TABLE [Empleados] (
    [Id] int NOT NULL IDENTITY,
    [Dni] nchar(8) NOT NULL,
    [Nombres] nvarchar(100) NOT NULL,
    [Apellidos] nvarchar(100) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [FechaIngreso] date NOT NULL,
    [AreaId] int NOT NULL,
    [CargoId] int NOT NULL,
    [SueldoBasico] decimal(12,2) NOT NULL,
    [TieneHijos] bit NOT NULL,
    [Regimen] nvarchar(10) NOT NULL,
    [AfpId] int NULL,
    [Cuspp] nvarchar(12) NULL,
    [Activo] bit NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Empleados] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Empleados_Afps_AfpId] FOREIGN KEY ([AfpId]) REFERENCES [Afps] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Empleados_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Empleados_Cargos_CargoId] FOREIGN KEY ([CargoId]) REFERENCES [Cargos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Asistencias] (
    [Id] int NOT NULL IDENTITY,
    [EmpleadoId] int NOT NULL,
    [Fecha] date NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    [HoraEntrada] time NULL,
    [HoraSalida] time NULL,
    [HorasExtra] decimal(4,2) NOT NULL,
    [Observacion] nvarchar(250) NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Asistencias] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Asistencias_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BoletasPago] (
    [Id] int NOT NULL IDENTITY,
    [PlanillaId] int NOT NULL,
    [EmpleadoId] int NOT NULL,
    [EmpleadoNombre] nvarchar(200) NOT NULL,
    [EmpleadoDni] nvarchar(8) NOT NULL,
    [Cargo] nvarchar(100) NOT NULL,
    [Area] nvarchar(100) NOT NULL,
    [Regimen] nvarchar(10) NOT NULL,
    [AfpNombre] nvarchar(50) NULL,
    [SueldoBasico] decimal(12,2) NOT NULL,
    [AsignacionFamiliar] decimal(12,2) NOT NULL,
    [HorasExtra25] decimal(6,2) NOT NULL,
    [HorasExtra35] decimal(6,2) NOT NULL,
    [MontoHorasExtra] decimal(12,2) NOT NULL,
    [DiasFalta] int NOT NULL,
    [DescuentoFaltas] decimal(12,2) NOT NULL,
    [TotalIngresos] decimal(12,2) NOT NULL,
    [Onp] decimal(12,2) NOT NULL,
    [AfpAporte] decimal(12,2) NOT NULL,
    [AfpPrima] decimal(12,2) NOT NULL,
    [AfpComision] decimal(12,2) NOT NULL,
    [Renta5ta] decimal(12,2) NOT NULL,
    [TotalDescuentos] decimal(12,2) NOT NULL,
    [NetoPagar] decimal(12,2) NOT NULL,
    [EsSalud] decimal(12,2) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_BoletasPago] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BoletasPago_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BoletasPago_Planillas_PlanillaId] FOREIGN KEY ([PlanillaId]) REFERENCES [Planillas] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Contratos] (
    [Id] int NOT NULL IDENTITY,
    [EmpleadoId] int NOT NULL,
    [Tipo] nvarchar(20) NOT NULL,
    [FechaInicio] date NOT NULL,
    [FechaFin] date NULL,
    [Sueldo] decimal(12,2) NOT NULL,
    [Estado] nvarchar(20) NOT NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Contratos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Contratos_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SolicitudesVacaciones] (
    [Id] int NOT NULL IDENTITY,
    [EmpleadoId] int NOT NULL,
    [FechaInicio] date NOT NULL,
    [FechaFin] date NOT NULL,
    [Motivo] nvarchar(250) NULL,
    [Estado] nvarchar(20) NOT NULL,
    [RevisadoPorId] int NULL,
    [FechaRevision] datetime2 NULL,
    [ComentarioRevision] nvarchar(250) NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_SolicitudesVacaciones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SolicitudesVacaciones_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(150) NOT NULL,
    [NombreCompleto] nvarchar(150) NOT NULL,
    [PasswordHash] nvarchar(100) NOT NULL,
    [Rol] nvarchar(20) NOT NULL,
    [Activo] bit NOT NULL,
    [EmpleadoId] int NULL,
    [CreadoEn] datetime2 NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Usuarios_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE SET NULL
);

CREATE UNIQUE INDEX [IX_Afps_Nombre] ON [Afps] ([Nombre]);

CREATE UNIQUE INDEX [IX_Areas_Nombre] ON [Areas] ([Nombre]);

CREATE UNIQUE INDEX [IX_Asistencias_EmpleadoId_Fecha] ON [Asistencias] ([EmpleadoId], [Fecha]);

CREATE INDEX [IX_BoletasPago_EmpleadoId] ON [BoletasPago] ([EmpleadoId]);

CREATE UNIQUE INDEX [IX_BoletasPago_PlanillaId_EmpleadoId] ON [BoletasPago] ([PlanillaId], [EmpleadoId]);

CREATE UNIQUE INDEX [IX_Cargos_Nombre] ON [Cargos] ([Nombre]);

CREATE INDEX [IX_Contratos_EmpleadoId] ON [Contratos] ([EmpleadoId]);

CREATE INDEX [IX_Empleados_Activo_AreaId] ON [Empleados] ([Activo], [AreaId]);

CREATE INDEX [IX_Empleados_AfpId] ON [Empleados] ([AfpId]);

CREATE INDEX [IX_Empleados_AreaId] ON [Empleados] ([AreaId]);

CREATE INDEX [IX_Empleados_CargoId] ON [Empleados] ([CargoId]);

CREATE UNIQUE INDEX [IX_Empleados_Dni] ON [Empleados] ([Dni]);

CREATE UNIQUE INDEX [IX_Planillas_Anio_Mes] ON [Planillas] ([Anio], [Mes]);

CREATE INDEX [IX_SolicitudesVacaciones_EmpleadoId_Estado] ON [SolicitudesVacaciones] ([EmpleadoId], [Estado]);

CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);

CREATE INDEX [IX_Usuarios_EmpleadoId] ON [Usuarios] ([EmpleadoId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261101202736_Inicial', N'10.0.14');

COMMIT;
GO

