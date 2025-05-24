Public Class Logger
    Public Shared Function GetDefaultLogger() As Logger
        return New Logger
    End Function

    Public Sub Write(loggerMessageLevel As LoggerMessageLevel, smsXMessageIdToSendByAts As String, id As Integer, target As String, resultMessage As String)
        Throw New NotImplementedException
    End Sub

    Public Sub Write(loggerMessageLevel As LoggerMessageLevel, smsXMessageIdToSendByAts As String, id As Integer, target As String)
        Throw New NotImplementedException
    End Sub
End Class

Public Enum LoggerMessageLevel

    Info
    [Error]
End Enum