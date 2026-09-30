ALTER TABLE MpPagosUnicos
ADD ExternalReference NVARCHAR(100) NULL;
GO

CREATE INDEX IX_MpPagosUnicos_ExternalReference ON MpPagosUnicos(ExternalReference);
GO
