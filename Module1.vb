'Date: 30/01/2020   Developed by: Amit Patil
'Program for testing AF Analysis backfilling / reclaulation 
'Target feamework shall be 4.6.2 Or later for AF (Include OSIsoft.AFSDK version 4.0.0.0 under references)
'Imports OSIsoft.AF.PI   'Namespace required for AF SDK PI Data Archive Server
Imports OSIsoft.AF       'Namespace required for AFErrors, PIsystems(AF)
Imports OSIsoft.AF.Asset 'Namespace required for AF SDK PI AF values, attributes
Imports OSIsoft.AF.Time  'Namespace required for AF SDK PI timerange, span
Imports OSIsoft.AF.Analysis 'Namespace required for AF Analysis
Imports OSIsoft.AF.Data 'Name spaces for AF data events
Module Module1
    'Global variables
    'Dim g_PIServers As OSIsoft.AF.PI.PIServers  'Global declaration of PI servers
    Dim g_AFServers As OSIsoft.AF.PISystems     'Global declaration of AF servers
    Dim myAFServer As PISystem  'AF Server
    Dim myAFDatabases As AFDatabases 'AF DB Collection
    Dim myAFDatabase As AFDatabase  'AF Database of interest
    Dim myAFElement As AFElement    'Target Element
    Public Pipe_Val_Action As String
    Public Pipe_Val_Timestamp As String

    Sub Main()
        Console.Title = "PI Analysis reclaulation test utility (AF SDK) - AP"
Selection:
        Call Analysis_Recalc()

        Console.WriteLine("Do you want to continue? Y/N: ")
        Dim YNselection As String = Console.ReadLine()
        If YNselection = "Y" Or YNselection = "y" Then
            GoTo Selection
        Else
            Console.WriteLine("Exiting the program...")
        End If

    End Sub
    Public Sub Analysis_Recalc()
        Dim temp_afsrv, temp_af_db, temp_af_element As String
        Dim temp_selection As Int16
        Dim timeRange As AFTimeRange
        Dim _afDataPipe As AFDataPipe
        Dim iobserver2 As AFConsoleDataReceiver
        Dim mode As AFAnalysisService.CalculationMode
        Dim AN_Service As AFAnalysisService
        Dim analyses As IEnumerable(Of AFAnalysis)
        Dim returnValue As Object

        Try
            Console.BackgroundColor = ConsoleColor.Black
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine() 'Blank line
            g_AFServers = New PISystems 'Instantiate a new AF servers object
            Console.Write("Enter AF Server name to be connected: ")
            temp_afsrv = Console.ReadLine()
            'temp_afsrv = "AZSG-D-DSPI"
            myAFServer = g_AFServers.Item(temp_afsrv)
            myAFServer.Connect()
            myAFDatabases = myAFServer.Databases
            Console.WriteLine() 'Blank line
            Console.Write("Enter AF DB: ")
            temp_af_db = Console.ReadLine()
            'temp_af_db = "Lab Analysis"
            myAFDatabase = myAFDatabases(temp_af_db)
            Console.WriteLine("AF Server name: " & myAFServer.Name & ", AF DB:" & myAFDatabase.Name)
            Console.Write("Enter target AF Element name: ") 'Path: Parent\Child format
            temp_af_element = Console.ReadLine()
            myAFElement = myAFDatabase.Elements(temp_af_element)
            Console.WriteLine() 'Blank line
            Console.WriteLine("Available Analyses are as follows:")
            For i = 0 To myAFElement.Analyses.Count - 1
                Dim tempstring As String = "" & vbTab & i + 1 & "." & vbTab & myAFElement.Analyses.Item(i).ToString
                Console.WriteLine(tempstring)
            Next
            Console.WriteLine() 'Blank line
            Console.Write("Select analysis number for recalculation: ")
            Dim calc_number As Int16 = Convert.ToInt16(Console.ReadLine()) - 1  'offset adjusted
            Dim analysis1 As AFAnalysis
            analysis1 = myAFElement.Analyses.Item(calc_number)

            Console.WriteLine() 'Blank line
            Console.WriteLine("Select option number.")
            Console.Write("1:Manual backfilling 2.Auto Trigegred backfilling (based on AF data-pipe monitoring OOO event): ")
            temp_selection = Convert.ToInt16(Console.ReadLine())

            AN_Service = myAFServer.AnalysisService
            analyses = {analysis1}  'Initilize analysis collection

            Select Case temp_selection
                Case 1
                    'Manual backfilling
                    Dim start_time, end_time As String
                    Console.Write("Select start time for recalc:")
                    start_time = Console.ReadLine()
                    Console.Write("Select end time for recalc:")
                    end_time = Console.ReadLine()
                    timeRange = AFTimeRange.Parse(start_time, end_time, myAFServer.ServerTime, Nothing)
                    mode = AFAnalysisService.CalculationMode.FillDataGaps 'Backfill manually
                    returnValue = AN_Service.QueueCalculation(analyses, timeRange, mode)
                    Console.WriteLine("Manual recalculation scheduled for the period " & "Start time-" &
                                      timeRange.StartTime.ToString & "End time-" & timeRange.EndTime.ToString)
                Case 2
                    'AF Datapipe based backfilling of out of order event
                    _afDataPipe = New AFDataPipe
                    'Dim instance As AFAnalysisRuleConfiguration 'Not required
                    'instance = analysis1.AnalysisRule.GetConfiguration 'Not required

                    Dim pipe_attributelist As AFAttributeList = analysis1.AnalysisRule.GetInputs
                    _afDataPipe.AddSignups(pipe_attributelist)
                    iobserver2 = New AFConsoleDataReceiver
                    _afDataPipe.Subscribe(iobserver2)   'Subscribe and pass iobserver reference
                    'Use Get Observer events 
                    Dim i As Int16
                    While i <= 300
                        'Console.WriteLine(i)
                        _afDataPipe.GetObserverEvents() 'Get AFdatapipe events then implement further logic
                        'Capture OOO event and perform recalculation for single new OOO event automatically (inserted or modified)
                        If Pipe_Val_Action = "Update" Then
                            timeRange = AFTimeRange.Parse(Pipe_Val_Timestamp, Pipe_Val_Timestamp, myAFServer.ServerTime, Nothing)
                            mode = AFAnalysisService.CalculationMode.FillDataGaps   'Backfill automatically for single OOO timestamp
                            returnValue = AN_Service.QueueCalculation(analyses, timeRange, mode)
                            Console.ForegroundColor = ConsoleColor.Red
                            Console.WriteLine("Automatic backfilling scheduled for time: " & Pipe_Val_Timestamp)
                            Console.ForegroundColor = ConsoleColor.White
                            Pipe_Val_Action = Nothing 'clear
                            Pipe_Val_Timestamp = Nothing 'clear
                        End If
                        Threading.Thread.Sleep(1000)    'Delay in ms , loop will exit after iMax value*delay seconds
                        i = i + 1
                    End While

                Case Else
                    'Nothing
            End Select

            myAFServer.Disconnect()
            Console.WriteLine("**********************************************")

        Catch ex As Exception
            Console.ForegroundColor = ConsoleColor.Red
            Console.WriteLine("Exception: " & ex.Message)
            Console.WriteLine("**********************************************")
        End Try
    End Sub

End Module

Public Class AFConsoleDataReceiver

    'Derived class of iObserver for AF Data Pipe
    'Refer - How to use the PIDataPipe or the AFDataPipe (PI Square ref)
    Implements IObserver(Of AFDataPipeEvent)

    Public Sub OnNext(value As AFDataPipeEvent) Implements IObserver(Of AFDataPipeEvent).OnNext
        Console.WriteLine("AFDataPipe event - Attribute Name: {0}, Action Type: {1}, Value {2}, TimeStamp: {3}", value.Value.Attribute.Name, value.Action.ToString(), value.Value.Value, value.Value.Timestamp.ToString())
        Pipe_Val_Action = value.Action.ToString()   'Public var
        Pipe_Val_Timestamp = value.Value.Timestamp.ToString 'Public var
        'Console.WriteLine("Action:" & Pipe_Val_Action & ", Timestamp: " & Pipe_Val_Timestamp)
    End Sub

    Public Sub OnError([error] As Exception) Implements IObserver(Of AFDataPipeEvent).OnError
        Console.WriteLine("Provider has sent an error.")

    End Sub

    Public Sub OnCompleted() Implements IObserver(Of AFDataPipeEvent).OnCompleted
        Console.WriteLine("Provider has terminated sending data.")
    End Sub

End Class
