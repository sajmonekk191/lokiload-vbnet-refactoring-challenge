' Test helper classes and enums for testing SendSMSMessage
Imports LokiLoad.Test.GSMConnector.GSM

#Region "Enumerations"
Public Enum MessageStatus
    [New] = 0
    Sending = 1
    Sent = 2
    [Error] = 3
End Enum

Public Enum MessageSmsGate
    ATS = 1
    Daktela = 2
End Enum

Public Enum MessageKind
    SMS = 0
    SMSCampaign = 1
End Enum

Public Enum SimSettingsType
    Standard = 0
    Campaigne = 1
End Enum

Public Enum LoggerMessageLevel
    Debug = 0
    Info = 1
    Warning = 2
    [Error] = 3
End Enum
#End Region

#Region "Core Classes"
' SimSettings is mock only - Message class comes from main project
Public Class SimSettings
    Public Property GatewayId As Integer
    Public Property Login As String
    Public Property Password As String
End Class
#End Region

#Region "Mock Services"
' Mock Logger - accepts both Integer and String for ID parameter
Public Class Logger
    Private Shared _instance As New Logger()

    Public Shared Function GetDefaultLogger() As Logger
        Return _instance
    End Function

    Public Sub Write(level As LoggerMessageLevel, format As String, ParamArray args() As Object)
        Dim message As String = format
        Try
            message = String.Format(format, args)
        Catch
            ' If format fails, just use the format string
        End Try
        TestLogCapture.AddLog(level, message)
    End Sub
End Class

' Mock SimSettingsManager
Public Class SimSettingsManager
    Public Shared Function GetSimSettingForMessage(environment As String, type As SimSettingsType) As SimSettings
        ' Always return Nothing to simulate no available SIM
        Return Nothing
    End Function
End Class
#End Region

#Region "Test Utilities"
' Test log capture utility
Public Class TestLogCapture
    Private Shared _logs As New List(Of LogEntry)

    Public Class LogEntry
        Public Property Level As LoggerMessageLevel
        Public Property Message As String
        Public Property Timestamp As DateTime = DateTime.Now
    End Class

    Public Shared Sub AddLog(level As LoggerMessageLevel, message As String)
        _logs.Add(New LogEntry With {.Level = level, .Message = message})
    End Sub

    Public Shared Sub Clear()
        _logs.Clear()
    End Sub

    Public Shared Function GetLogs() As List(Of LogEntry)
        Return _logs.ToList()
    End Function

    Public Shared Function GetLogCount() As Integer
        Return _logs.Count
    End Function
End Class
#End Region