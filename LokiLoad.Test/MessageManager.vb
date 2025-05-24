Imports LokiLoad.Test.GSMConnector.GSM
Imports System.Configuration

Public Class MessageManager

#Region "Configuration Properties"
    Private Shared ReadOnly Property ATSLogin As String = ConfigurationManager.AppSettings("ATSLogin")
    Private Shared ReadOnly Property ATSPassword As String = ConfigurationManager.AppSettings("ATSPassword")
    Private Shared ReadOnly Property DaktelaLogin As String = ConfigurationManager.AppSettings("DaktelaLogin")
    Private Shared ReadOnly Property DaktelaPassword As String = ConfigurationManager.AppSettings("DaktelaPassword")

    Public Shared Property UseDaktela As Boolean
#End Region

#Region "Constants"
    Private Const InvalidPhoneErrorMessage As String = "Nepodarilo se odeslat SMS Ex: Telefon nema nutnych 9 znaku"
    Private Const DaktelaGatewayId As Integer = 100
    Private Const MinPhoneNumberLength As Integer = 9
#End Region

    ''' <summary>
    ''' Sends SMS message to specified recipients using configured gateway
    ''' </summary>
    ''' <param name="myMessage">Message to send</param>
    ''' <exception cref="ArgumentNullException">When message is null</exception>
    ''' <exception cref="Exception">When no gateway is available</exception>
    Private Shared Sub SendSMSMessage(ByVal myMessage As Message)
        If myMessage Is Nothing Then
            Throw New ArgumentNullException(NameOf(myMessage))
        End If

        Dim recipients = GetRecipients(myMessage)
        If Not recipients.Any() Then Return ' No recipients to process

        Dim gateway = ResolveGateway(myMessage)
        ExecuteSending(myMessage, recipients, gateway)
    End Sub

#Region "Recipient Resolution"
    Private Shared Function GetRecipients(message As Message) As IEnumerable(Of String)
        Return If(IsDebugModeActive(), GetDebugRecipients(), GetProductionRecipients(message))
    End Function

    Private Shared Function IsDebugModeActive() As Boolean
        Dim debugMode = ConfigurationManager.AppSettings("DebugMode")
        Return String.Equals(debugMode, "true", StringComparison.OrdinalIgnoreCase) AndAlso
               Not String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings("Messages.GSM.Debug.To"))
    End Function

    Private Shared Function GetDebugRecipients() As IEnumerable(Of String)
        Dim debugTargets = ConfigurationManager.AppSettings("Messages.GSM.Debug.To")
        Return debugTargets.Split({","c, ";"c}, StringSplitOptions.RemoveEmptyEntries)
    End Function

    Private Shared Function GetProductionRecipients(message As Message) As IEnumerable(Of String)
        Dim recipients As New List(Of String)

        AddIfNotEmpty(recipients, message.Target)
        AddIfNotEmpty(recipients, message.TargetCC)
        AddIfNotEmpty(recipients, message.TargetBCC)

        Return recipients
    End Function

    Private Shared Sub AddIfNotEmpty(list As List(Of String), value As String)
        If Not String.IsNullOrWhiteSpace(value) Then
            list.Add(value.Trim())
        End If
    End Sub
#End Region

#Region "Gateway Resolution"
    Private Shared Function ResolveGateway(message As Message) As SmsGateway
        ' Explicit gateway takes precedence
        If message.SmsGate.HasValue Then
            Return CreateGateway(message.SmsGate.Value)
        End If

        ' Auto-select based on configuration
        Return AutoSelectGateway(message)
    End Function

    Private Shared Function CreateGateway(smsGate As MessageSMSGate) As SmsGateway
        Select Case smsGate
            Case MessageSMSGate.ATS
                Return New SmsGateway(ATSLogin, ATSPassword, "ATS")
            Case MessageSMSGate.Daktela
                Return New SmsGateway(DaktelaLogin, DaktelaPassword, "Daktela")
            Case Else
                Throw New NotSupportedException($"SMS gate '{smsGate}' is not supported")
        End Select
    End Function

    Private Shared Function AutoSelectGateway(message As Message) As SmsGateway
        Dim simType = DetermineSimType(message)
        Dim simSettings = SimSettingsManager.GetSimSettingForMessage(message.Environment, simType)

        If simSettings IsNot Nothing Then
            message.GatewayId = simSettings.GatewayId
            Return New SmsGateway(simSettings.Login, simSettings.Password, "SIM")
        End If

        ' Try fallback
        If CanUseDaktelaAsFallback(simType) Then
            message.GatewayId = DaktelaGatewayId
            Return New SmsGateway(DaktelaLogin, DaktelaPassword, "Daktela")
        End If

        ' No gateway available
        Throw CreateNoGatewayException(message, simType)
    End Function

    Private Shared Function DetermineSimType(message As Message) As SimSettingsType
        Return If(message.Kind = MessageKind.SMSCampaign,
                  SimSettingsType.Campaigne,
                  SimSettingsType.Standard)
    End Function

    Private Shared Function CanUseDaktelaAsFallback(simType As SimSettingsType) As Boolean
        Return simType = SimSettingsType.Standard AndAlso UseDaktela
    End Function

    Private Shared Function CreateNoGatewayException(message As Message, simType As SimSettingsType) As Exception
        Dim errorMsg = $"Error sending SMS - Not more SIM {simType} space."
        Logger.GetDefaultLogger.Write(LoggerMessageLevel.Error,
            errorMsg & " Message-ID={0} to {1}", message.Id, message.Target)
        Return New InvalidOperationException(errorMsg)
    End Function
#End Region

#Region "SMS Sending"
    Private Shared Sub ExecuteSending(message As Message, recipients As IEnumerable(Of String), gateway As SmsGateway)
        Using service As New GSMServiceSoapClient()
            For Each recipient In recipients
                SendIndividualSms(service, message, recipient, gateway)
            Next
        End Using
    End Sub

    Private Shared Sub SendIndividualSms(service As GSMServiceSoapClient, message As Message,
                                        recipient As String, gateway As SmsGateway)
        Try
            ValidatePhoneNumber(recipient)

            message.DateOfSend = DateTime.Now
            Dim response = SendSmsRequest(service, message, recipient, gateway)
            ProcessResponse(message, recipient, response, gateway)

        Catch ex As InvalidPhoneNumberException
            LogInvalidPhoneNumber(message, recipient, ex)
        Catch ex As Exception When Not TypeOf ex Is InvalidOperationException
            ' Wrap unexpected exceptions with context
            Throw New InvalidOperationException(
                $"Failed to send SMS {message.Id} to {recipient} via {gateway.Name}", ex)
        End Try
    End Sub

    Private Shared Sub ValidatePhoneNumber(phoneNumber As String)
        ' Basic validation - should be extracted to validator class in real implementation
        If String.IsNullOrWhiteSpace(phoneNumber) OrElse phoneNumber.Length < MinPhoneNumberLength Then
            Throw New InvalidPhoneNumberException(InvalidPhoneErrorMessage)
        End If
    End Sub

    Private Shared Function SendSmsRequest(service As GSMServiceSoapClient, message As Message,
                                          recipient As String, gateway As SmsGateway) As WSMessageSendResponse
        Return service.SendSMS(
            gateway.Login, gateway.Password, recipient, message.Body,
            "", "", "", True, Nothing, Nothing)
    End Function

    Private Shared Sub ProcessResponse(message As Message, recipient As String,
                                      response As WSMessageSendResponse, gateway As SmsGateway)
        If response.Result Then
            ProcessSuccessResponse(message, recipient, response, gateway)
        Else
            ProcessErrorResponse(message, recipient, response, gateway)
        End If
    End Sub

    Private Shared Sub ProcessSuccessResponse(message As Message, recipient As String,
                                            response As WSMessageSendResponse, gateway As SmsGateway)
        message.ExternalId = response.IdMessage
        message.Status = MessageStatus.Sending
        Save(message, False)

        LogSuccess(message, recipient, gateway)
    End Sub

    Private Shared Sub ProcessErrorResponse(message As Message, recipient As String,
                                          response As WSMessageSendResponse, gateway As SmsGateway)
        message.Status = MessageStatus.Error
        Save(message, False)

        LogError(message, recipient, response.ResultMessage, gateway)
    End Sub
#End Region

#Region "Logging"
    Private Shared Sub LogSuccess(message As Message, recipient As String, gateway As SmsGateway)
        Dim format = If(gateway.Name = "ATS" OrElse gateway.Name = "Daktela",
                       "SMS X-Message-ID={0} to {1} send by " & gateway.Name & ".",
                       "SMS X-Message-ID={0} to {1} send.")

        Logger.GetDefaultLogger.Write(LoggerMessageLevel.Info, format, message.Id, recipient)
    End Sub

    Private Shared Sub LogError(message As Message, recipient As String, errorMessage As String, gateway As SmsGateway)
        Logger.GetDefaultLogger.Write(LoggerMessageLevel.Error,
            "Error sending SMS X-Message-ID={0} by " & gateway.Name & " to {1}, {2}",
            message.Id, recipient, errorMessage)
    End Sub

    Private Shared Sub LogInvalidPhoneNumber(message As Message, recipient As String, ex As Exception)
        Logger.GetDefaultLogger.Write(LoggerMessageLevel.Info,
            "Message Id={0} to {1} - " & ex.Message, message.Id, recipient)

        message.Status = MessageStatus.Error
        Save(message, False)
    End Sub
#End Region

    Private Shared Function Save(myMessage As Message, b As Boolean) As WSMessageSendResponse
        ' TODO: Implement persistence logic
        Throw New NotImplementedException()
    End Function

End Class

#Region "Supporting Classes"
''' <summary>
''' Represents SMS gateway configuration
''' </summary>
Friend NotInheritable Class SmsGateway
    Public ReadOnly Property Login As String
    Public ReadOnly Property Password As String
    Public ReadOnly Property Name As String

    Public Sub New(login As String, password As String, name As String)
        If String.IsNullOrWhiteSpace(login) Then Throw New ArgumentException("Login cannot be empty", NameOf(login))
        If String.IsNullOrWhiteSpace(password) Then Throw New ArgumentException("Password cannot be empty", NameOf(password))
        If String.IsNullOrWhiteSpace(name) Then Throw New ArgumentException("Name cannot be empty", NameOf(name))

        Me.Login = login
        Me.Password = password
        Me.Name = name
    End Sub
End Class

''' <summary>
''' Exception thrown when phone number validation fails
''' </summary>
Public Class InvalidPhoneNumberException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class
#End Region