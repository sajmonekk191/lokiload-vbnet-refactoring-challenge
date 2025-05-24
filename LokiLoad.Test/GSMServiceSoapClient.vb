
Namespace GSMConnector.GSM

    Public Class GSMServiceSoapClient 
        Implements IDisposable
        Public Function SendSMS(daktelaGsmLogin As String, daktelaGsmPassword As String, target As String, body As String, empty As String, s As String, empty1 As String, b As Boolean, o As Object, o1 As Object) As WSMessageSendResponse
            Throw New NotImplementedException
        End Function


        Public Sub Dispose() Implements IDisposable.Dispose
            
        End Sub
    End Class
    
    Public Class WSMessageSendResponse
        Public Property Result As Boolean

        Public Property IdMessage As String

        Public Property ResultMessage As String

    End Class
    
End Namespace
