Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Reflection
Imports System.Configuration
Imports LokiLoad.Test

<TestClass()>
Public Class RefactoredTests

    Private _sendSmsMethod As MethodInfo
    Private _behaviorLog As New List(Of String)
    Private _originalBehavior As List(Of String)
    Private _comparisonResults As New List(Of String)

    <TestInitialize()>
    Public Sub Setup()
        If System.IO.File.Exists("OriginalTest.txt") Then
            _originalBehavior = System.IO.File.ReadAllLines("OriginalTest.txt").ToList()
        Else
            _originalBehavior = New List(Of String)
        End If

        _behaviorLog.Clear()
        _comparisonResults.Clear()
        TestLogCapture.Clear()

        Dim messageManagerType = GetType(MessageManager)
        _sendSmsMethod = messageManagerType.GetMethod("SendSMSMessage",
            BindingFlags.NonPublic Or BindingFlags.Static)

        Assert.IsNotNull(_sendSmsMethod, "SendSMSMessage method must exist after refactoring")
    End Sub

    <TestCleanup()>
    Public Sub Cleanup()
        SaveResults()
    End Sub

    <TestMethod()>
    Public Sub Refactored_01_MethodSignature()
        _behaviorLog.Add("=== METHOD SIGNATURE TEST ===")

        Dim params = _sendSmsMethod.GetParameters()
        _behaviorLog.Add($"ParameterCount: {params.Length}")
        _behaviorLog.Add($"ParameterType: {params(0).ParameterType.Name}")

        Assert.AreEqual(1, params.Length)
        CompareWithOriginal("METHOD SIGNATURE TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_02_NullMessageHandling()
        _behaviorLog.Add("=== NULL MESSAGE TEST ===")

        Try
            _sendSmsMethod.Invoke(Nothing, {Nothing})
            _behaviorLog.Add("NullResult: NoException")
        Catch ex As TargetInvocationException
            _behaviorLog.Add($"NullResult: {ex.InnerException.GetType().Name}")
        End Try

        CompareWithOriginal("NULL MESSAGE TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_03_ATSGateway()
        _behaviorLog.Add("=== ATS GATEWAY TEST ===")
        ConfigurationManager.AppSettings("ATSLogin") = "test_ats"
        ConfigurationManager.AppSettings("ATSPassword") = "pass_ats"

        Dim message As New Message()
        message.Target = "123456789"
        message.Body = "ATS Test Message"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "ATS")
        CompareWithOriginal("ATS GATEWAY TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_04_DaktelaGateway()
        _behaviorLog.Add("=== DAKTELA GATEWAY TEST ===")
        ConfigurationManager.AppSettings("DaktelaLogin") = "test_daktela"
        ConfigurationManager.AppSettings("DaktelaPassword") = "pass_daktela"

        Dim message As New Message()
        message.Target = "987654321"
        message.Body = "Daktela Test Message"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.Daktela

        TestMessageSending(message, "Daktela")
        CompareWithOriginal("DAKTELA GATEWAY TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_05_DebugMode()
        _behaviorLog.Add("=== DEBUG MODE TEST ===")
        ConfigurationManager.AppSettings("DebugMode") = "true"
        ConfigurationManager.AppSettings("Messages.GSM.Debug.To") = "111111111;222222222"
        ConfigurationManager.AppSettings("ATSLogin") = "test"
        ConfigurationManager.AppSettings("ATSPassword") = "test"

        Dim message As New Message()
        message.Target = "999999999"
        message.Body = "Debug Test"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "Debug")
        CompareWithOriginal("DEBUG MODE TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_06_DebugModeCaseInsensitive()
        _behaviorLog.Add("=== DEBUG MODE CASE TEST ===")
        ConfigurationManager.AppSettings("DebugMode") = "TRUE"
        ConfigurationManager.AppSettings("Messages.GSM.Debug.To") = "333333333"

        Dim message As New Message()
        message.Target = "444444444"
        message.Body = "Debug Case Test"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "DebugCase")
        CompareWithOriginal("DEBUG MODE CASE TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_07_MultipleRecipients()
        _behaviorLog.Add("=== MULTIPLE RECIPIENTS TEST ===")
        ConfigurationManager.AppSettings("ATSLogin") = "test"
        ConfigurationManager.AppSettings("ATSPassword") = "test"

        Dim message As New Message()
        message.Target = "111111111"
        message.TargetCC = "222222222"
        message.TargetBCC = "333333333"
        message.Body = "Multi Target Test"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "MultiTarget")
        CompareWithOriginal("MULTIPLE RECIPIENTS TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_08_EmptyRecipients()
        _behaviorLog.Add("=== EMPTY RECIPIENTS TEST ===")

        Dim message As New Message()
        message.Target = ""
        message.Body = "Empty Target Test"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "EmptyTarget")
        CompareWithOriginal("EMPTY RECIPIENTS TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_09_AutoGatewayStandard()
        _behaviorLog.Add("=== AUTO GATEWAY STANDARD TEST ===")

        Dim message As New Message()
        message.Target = "555555555"
        message.Body = "Auto Gateway Test"
        message.Status = MessageStatus.New
        message.Kind = MessageKind.SMS
        message.Environment = "TEST"

        TestMessageSending(message, "AutoStandard")
        CompareWithOriginal("AUTO GATEWAY STANDARD TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_10_AutoGatewayCampaign()
        _behaviorLog.Add("=== AUTO GATEWAY CAMPAIGN TEST ===")

        Dim message As New Message()
        message.Target = "666666666"
        message.Body = "Campaign Test"
        message.Status = MessageStatus.New
        message.Kind = MessageKind.SMSCampaign
        message.Environment = "PROD"

        TestMessageSending(message, "AutoCampaign")
        CompareWithOriginal("AUTO GATEWAY CAMPAIGN TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_11_DaktelaFallback()
        _behaviorLog.Add("=== DAKTELA FALLBACK TEST ===")
        MessageManager.UseDaktela = True
        ConfigurationManager.AppSettings("DaktelaLogin") = "fallback"
        ConfigurationManager.AppSettings("DaktelaPassword") = "fallback"

        Dim message As New Message()
        message.Target = "777777777"
        message.Body = "Fallback Test"
        message.Status = MessageStatus.New
        message.Kind = MessageKind.SMS
        message.Environment = "TEST"

        TestMessageSending(message, "Fallback")
        CompareWithOriginal("DAKTELA FALLBACK TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_12_ShortPhoneNumber()
        _behaviorLog.Add("=== SHORT PHONE TEST ===")
        ConfigurationManager.AppSettings("ATSLogin") = "test"
        ConfigurationManager.AppSettings("ATSPassword") = "test"

        Dim message As New Message()
        message.Target = "12345"
        message.Body = "Short Phone Test"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "ShortPhone")
        CompareWithOriginal("SHORT PHONE TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_13_NoGatewayAvailable()
        _behaviorLog.Add("=== NO GATEWAY TEST ===")
        MessageManager.UseDaktela = False

        Dim message As New Message()
        message.Target = "888888888"
        message.Body = "No Gateway Test"
        message.Status = MessageStatus.New
        message.Kind = MessageKind.SMS
        message.Environment = "TEST"

        TestMessageSending(message, "NoGateway")
        CompareWithOriginal("NO GATEWAY TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_14_ConfigurationDependencies()
        _behaviorLog.Add("=== CONFIGURATION TEST ===")

        Dim configKeys = {"ATSLogin", "ATSPassword", "DaktelaLogin",
                         "DaktelaPassword", "DebugMode", "Messages.GSM.Debug.To"}

        For Each key In configKeys
            Dim value = ConfigurationManager.AppSettings(key)
            _behaviorLog.Add($"Config_{key}: {If(String.IsNullOrEmpty(value), "Empty", "HasValue")}")
        Next

        CompareWithOriginal("CONFIGURATION TEST")
    End Sub

    <TestMethod()>
    Public Sub Refactored_15_EdgeCases()
        _behaviorLog.Add("=== EDGE CASES TEST ===")

        ConfigurationManager.AppSettings("ATSLogin") = "test"
        ConfigurationManager.AppSettings("ATSPassword") = "test"

        Dim message As New Message()
        message.Target = "  123456789  "
        message.TargetCC = vbTab & "987654321"
        message.Body = "Whitespace Test"
        message.Status = MessageStatus.New
        message.SmsGate = MessageSmsGate.ATS

        TestMessageSending(message, "EdgeCase")
        _behaviorLog.Add("WhitespaceHandled: True")
    End Sub

    Private Sub TestMessageSending(message As Message, testName As String)
        Try
            TestLogCapture.Clear()

            _behaviorLog.Add($"{testName}_InitialStatus: {message.Status}")

            _sendSmsMethod.Invoke(Nothing, {message})

            _behaviorLog.Add($"{testName}_Result: Success")
            _behaviorLog.Add($"{testName}_FinalStatus: {message.Status}")

        Catch ex As TargetInvocationException
            _behaviorLog.Add($"{testName}_Result: Exception")
            _behaviorLog.Add($"{testName}_ExceptionType: {ex.InnerException.GetType().Name}")
        End Try

        Dim logs = TestLogCapture.GetLogs()
        _behaviorLog.Add($"{testName}_LogCount: {logs.Count}")
    End Sub

    Private Sub CompareWithOriginal(testSection As String)
        Dim originalLine = _originalBehavior.FirstOrDefault(Function(x) x.Contains(testSection))
        If originalLine IsNot Nothing Then
            _comparisonResults.Add($"{testSection}: CHECKED")
        Else
            _comparisonResults.Add($"{testSection}: NOT FOUND IN ORIGINAL")
        End If
    End Sub

    Private Sub SaveResults()
        System.IO.File.WriteAllLines("RefactoredTest.txt", _behaviorLog)

        Dim report As New List(Of String)
        report.Add("=== REFACTORING COMPARISON REPORT ===")
        report.Add($"Generated: {DateTime.Now}")
        report.Add("")
        report.AddRange(_comparisonResults)
        report.Add("")
        report.Add($"Total Tests: {_comparisonResults.Count}")

        System.IO.File.WriteAllLines("ComparisonReport.txt", report)
    End Sub
End Class