-- FIRMALAR.VARSAYILAN_PARA_BIRIMI: kayıt sırasında NULL insert hatasını önlemek için varsayılan TRY
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.FIRMALAR', N'VARSAYILAN_PARA_BIRIMI') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.FIRMALAR')
          AND c.name = N'VARSAYILAN_PARA_BIRIMI'
    )
    BEGIN
        ALTER TABLE [dbo].[FIRMALAR]
            ADD CONSTRAINT [DF_FIRMALAR_VARSAYILAN_PARA_BIRIMI] DEFAULT (N'TRY') FOR [VARSAYILAN_PARA_BIRIMI];
    END
END
GO
