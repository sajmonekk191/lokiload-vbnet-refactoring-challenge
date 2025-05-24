Public Class Message
    Public Property Target As String

    Public Property TargetCC As String

    Public Property TargetBCC As String

    Public Property DateOfSend As Date

    Public Property ExternalId As String

    Public Property Status As MessageStatus

    Public Property SmsGate As Nullable(Of MessageSMSGate)

    Public Property GatewayId As Integer

    Public Property Kind As Nullable(Of MessageKind)

    Public Property Body As String

    Public Property Id As Integer

    Public Property Environment As Object

End Class

Public Enum MessageSMSGate

    ATS
    Daktela
End Enum

Public Enum MessageStatus

    Sending
    [Error]
End Enum

Public Enum MessageKind

    SMSCampaign
End Enum