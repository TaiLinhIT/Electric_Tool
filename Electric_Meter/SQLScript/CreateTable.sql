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
