USE [kartris29006_swatch_options]
GO

ALTER TABLE [dbo].[tblKartrisOptionGroups] DROP CONSTRAINT [CK_OptionGroupsValidation]
GO

ALTER TABLE [dbo].[tblKartrisOptionGroups] ADD  CONSTRAINT [CK_OptionGroupsValidation] CHECK  (([OPTG_OptionDisplayType]='d' OR [OPTG_OptionDisplayType]='l' OR [OPTG_OptionDisplayType]='s'))
GO



