CREATE TABLE [dbo].[dv_Machine](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Port] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Line] [nvarchar](100) NOT NULL,
	[Baudrate] [int] NOT NULL,
	[Address] [int] NOT NULL,
	[LineCode] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


USE [PowerTempWatch]

CREATE TABLE [dbo].[dv_ElectricDataTemp](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdMachine] int NOT NULL,
	[Ia] [float] NULL,
	[Ib] [float] NULL,
	[Ic] [float] NULL,
	[Pt] [float] NULL,
	[Pa] [float] NULL,
	[Pb] [float] NULL,
	[Pc] [float] NULL,
	[Ua] [float] NULL,
	[Ub] [float] NULL,
	[Uc] [float] NULL,
	[Exp] [float] NULL,
	[Imp] [float] NULL,
	[TotalElectric] [float] NULL,
	[UploadDate] [datetime] NOT NULL,
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[SensorData](
	[logid] [int] NOT NULL,
	[devid] [int] NOT NULL,
	[codeid] [int] NOT NULL,
	[value] [float] NOT NULL,
	[day] [datetime] NOT NULL
) ON [PRIMARY]



CREATE TABLE [dbo].[SesnsorType](
	[typeid] [int] NOT NULL,
	[name] [varchar](20) NOT NULL
) ON [PRIMARY]


CREATE TABLE [dbo].[dv_FactoryAddress_Configs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Factory] [nvarchar](50) NOT NULL,
	[Assembling] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]




USE [PowerTempWatch]
GO

INSERT INTO [dbo].[dv_Machine]
           ([Port]
           ,[Name]
           ,[Line]
           ,[Baudrate]
           ,[Address]
           ,[LineCode])
VALUES
    -- Nh車m A
    ('COM4', 'Machine 1', 'A', 1200, 1, 'H'),
    ('COM4', 'Machine 2', 'A', 1200, 2, 'H'),
    ('COM4', 'Machine 3', 'A', 1200, 3, 'H'),
    ('COM4', 'Machine 4', 'A', 1200, 4, 'H'),
    ('COM4', 'Machine 5', 'A', 1200, 5, 'H'),
    ('COM4', 'Machine 6', 'A', 1200, 6, 'H'),
    ('COM4', 'Machine 7', 'A', 1200, 7, 'H'),
    ('COM4', 'Machine 8', 'A', 1200, 8, 'H'),

    -- Nh車m B
    ('COM4', 'Machine 9', 'B', 1200, 9, 'H'),
    ('COM4', 'Machine 10', 'B', 1200, 10, 'H'),
    ('COM4', 'Machine 11', 'B', 1200, 11, 'H'),
    ('COM4', 'Machine 12', 'B', 1200, 12, 'H'),
    ('COM4', 'Machine 13', 'B', 1200, 13, 'H'),
    ('COM4', 'Machine 14', 'B', 1200, 14, 'H'),
    ('COM4', 'Machine 15', 'B', 1200, 15, 'H'),
    ('COM4', 'Machine 16', 'B', 1200, 16, 'H'),

    -- Nh車m C
    ('COM4', 'Machine 17', 'C', 1200, 17, 'H'),
    ('COM4', 'Machine 18', 'C', 1200, 18, 'H'),
    ('COM4', 'Machine 19', 'C', 1200, 19, 'H'),
    ('COM4', 'Machine 20', 'C', 1200, 20, 'H'),
    ('COM4', 'Machine 21', 'C', 1200, 21, 'H'),
    ('COM4', 'Machine 22', 'C', 1200, 22, 'H'),
    ('COM4', 'Machine 23', 'C', 1200, 23, 'H'),
    ('COM4', 'Machine 24', 'C', 1200, 24, 'H'),

    -- Nh車m D
    ('COM4', 'Machine 25', 'D', 1200, 25, 'H'),
    ('COM4', 'Machine 26', 'D', 1200, 26, 'H'),
    ('COM4', 'Machine 27', 'D', 1200, 27, 'H'),
    ('COM4', 'Machine 28', 'D', 1200, 28, 'H'),
    ('COM4', 'Machine 29', 'D', 1200, 29, 'H'),
    ('COM4', 'Machine 30', 'D', 1200, 30, 'H'),
    ('COM4', 'Machine 31', 'D', 1200, 31, 'H'),
    ('COM4', 'Machine 32', 'D', 1200, 32, 'H');
GO
