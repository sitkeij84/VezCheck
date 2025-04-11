// VezCheck – Digital Checklist for Nuclear Control Room Operations (Console-based F# Application)

open System
open System.IO
open System.Text
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Newtonsoft.Json.Serialization

// ====== Data Types ======
type InspectionStatus =
    | OK
    | FAIL
    | ERROR
    | NOT_APPLICABLE

type Priority = Low | Medium | High

type ChecklistItem = {
    Description: string
    Status: InspectionStatus
    Comment: string option
    Priority: Priority
}

type Inspection = {
    Date: DateTime
    Type: string
    Inspector: string
    Items: ChecklistItem list
}

// ====== Predefined Checklists ======
let dailyChecklist = [
    "Primary cooling system pressure check"
    "Secondary cooling circuit temperature control"
    "Pump feedback signal verification"
    "UPS power supply status"
    "Main control panel LED indicators"
    "Alarm system test"
    "Control rod position feedback"
    "Heat exchanger inlet/outlet temperature"
    "Feedwater system pressure monitoring"
    "Diesel generator standby check"
    "Auto cooling fan operation test"
    "Hydraulic valve status lights"
    "Reactor zone temperature sensor readout"
    "Access door interlock system test"
    "Control console ventilation check"
    "Instrumentation grounding check"
    "Coolant loop circulation flow check"
    "Primary loop water chemistry sampling"
    "PLC module error light verification"
    "Reactor water level display check"
    "Annunciator light test"
    "Radiation badge reader status"
    "Emergency evacuation signage illumination"
    "Network status indicator check"
    "Operator shift log timestamp"
    "Daily SCADA connectivity check"
    "Reactor mode switch position verification"
    "Lighting intensity measurement in control room"
    "End-of-shift checklist handover"
    "Fire suppression system panel light check"
    "Redundant input power path validation"
    "Sensor readout verification on display units"
    "Operator presence acknowledgement sensor check"
    "Control terminal label integrity check"
    "Status light power cycling test"
    "Manual override key switch test"
    "Data logger sync confirmation"
    "Wall-mounted equipment inspection"
    "Floor safety marking review"
    "SCADA polling rate monitor"
]

let weeklyChecklist = [
    "Diesel generator automatic test"
    "Mechanical testing of safety valves"
    "Backup power source activation test"
    "Emergency shutdown button test"
    "Simulated emergency stop procedure"
    "Radiation monitor diagnostics"
    "SCADA system data backup"
    "Manual control panel button testing"
    "Compressor system operational check"
    "Cooling water tank level verification"
    "Redundant system crossover functionality"
    "Reactor building pressure integrity test"
    "Airlock door mechanical seal inspection"
    "Electrical relay board test"
    "Motor control cabinet temperature check"
    "Fire extinguisher pressure gauge test"
    "Lighting panel functional check"
    "System log review and signoff"
    "Emergency radio test"
    "Physical key inventory check"
    "Uninterruptible power interface test"
    "Weekly mechanical vibration trend review"
    "Hot standby systems checklist"
    "Reactor water conductivity sampling"
    "Control rod step position logging"
    "Battery charger state-of-health test"
    "Ventilation duct sensor calibration"
    "Seismic sensor self-test"
    "Physical access panel status check"
    "Labeling and tagging compliance verification"
    "Training log verification"
    "Backup procedure binder audit"
    "Operations crew check-in verification"
]

let monthlyChecklist = [
    "Calibration of radiation sensors"
    "Fire protection system full function test"
    "Load test of backup diesel generator"
    "Inspection of control system updates"
    "Pressure test of charging/discharging system"
    "UPS battery replacement or charge check"
    "Surveillance camera system test"
    "Fan vibration and noise level measurement"
    "Control console display brightness test"
    "Emergency lighting system full test"
    "HVAC filter replacement"
    "Battery room temperature logging"
    "Containment vessel seal inspection"
    "Backup SCADA node restart test"
    "Cable integrity test in high-voltage area"
    "Documentation revision verification"
    "Floor marking and safety signage check"
    "Instrumentation cabinet cleanliness check"
    "Critical spare parts availability check"
    "Long-term maintenance task scheduling"
    "Reactor floor decontamination procedure"
    "Fire door closure timer verification"
    "Pressure gauge calibration log check"
    "Monthly incident simulation drill log"
    "Radiation shielding panel visual check"
    "Hazardous area gas detector test"
    "Reactor safety case documentation signoff"
    "Dry run of quarterly emergency drill"
    "Test of satellite communication link"
    "Review of all active alarms"
    "Monthly operator skills review"
    "External inspection coordination checklist"
    "Temporary work permit audit"
]

let getChecklist checklistType =
    match checklistType with
    | "daily" -> dailyChecklist
    | "weekly" -> weeklyChecklist
    | "monthly" -> monthlyChecklist
    | _ -> []

// ====== Console UI Helpers ======
let writeColored color text =
    Console.ForegroundColor <- color
    printfn "%s" text
    Console.ResetColor()

let drawSeparator () =
    writeColored ConsoleColor.DarkGray "-------------------------------------------"

let prompt msg =
    Console.Write(msg + ": ")
    Console.ReadLine().Trim()

let rec askStatus () =
    let input = prompt "Status? (ok/fail/error/na)"
    match input.Trim().ToLower() with
    | "ok" -> OK
    | "fail" -> FAIL
    | "error" -> ERROR
    | "na" | "n/a" -> NOT_APPLICABLE
    | _ ->
        writeColored ConsoleColor.Red "Invalid input. Type 'ok', 'fail', 'error' or 'na'."
        askStatus()

// ====== Statistics Output ======
let summarize inspection =
    drawSeparator()
    writeColored ConsoleColor.Blue "Inspection Summary:"
    let total = List.length inspection.Items
    let ok = inspection.Items |> List.filter (fun i -> i.Status = OK) |> List.length
    let fail = inspection.Items |> List.filter (fun i -> i.Status = FAIL) |> List.length
    let error = inspection.Items |> List.filter (fun i -> i.Status = ERROR) |> List.length
    let withComments = inspection.Items |> List.filter (fun i -> i.Comment.IsSome) |> List.length
    let percent = float ok / float total * 100.0
    printfn "Type: %s" inspection.Type
    printfn "Inspector: %s" inspection.Inspector
    printfn "Date: %O" inspection.Date
    printfn "Total items: %d" total
    printfn "OK: %d, FAIL: %d, ERROR: %d" ok fail error
    printfn "Completion rate (OK only): %.1f%%" percent
    printfn "Items with comments: %d" withComments
    drawSeparator()

let listResults inspection =
    writeColored ConsoleColor.Green "✔ Completed Items:"
    inspection.Items |> List.iter (fun i -> if i.Status = OK then printfn " - %s" i.Description)
    writeColored ConsoleColor.Red "✖ Failed or Error Items:"
    inspection.Items |> List.iter (fun i -> if i.Status <> OK then printfn " - %s (%A)" i.Description i.Status)
    writeColored ConsoleColor.Yellow "📝 Comments:"
    inspection.Items |> List.iter (fun i -> match i.Comment with | Some c -> printfn " - %s: %s" i.Description c | None -> ())

// ====== Inspection Process ======
let assignPriority (description: string) =
    if description.ToLower().Contains("emergency") then High
    elif description.ToLower().Contains("test") then Medium
    else Low

let runInspection (checkListType: string) : Inspection option =
    drawSeparator()
    writeColored ConsoleColor.Yellow ($"Starting {checkListType.ToUpper()} inspection...")
    drawSeparator()

    let mutable index = 0
    let descriptions = getChecklist checkListType
    let items = ResizeArray<ChecklistItem>()
    let mutable cancel = false

    while index < descriptions.Length && not cancel do
        let desc = descriptions.[index]
        writeColored ConsoleColor.Cyan ($"
Item {index + 1}/{descriptions.Length}: {desc}")
        let status = askStatus()
        let comment = prompt "Comment (optional)"

        items.Add {
            Description = desc
            Status = status
            Comment = if comment = "" then None else Some comment
            Priority = assignPriority desc
        }

        let nav = prompt "Next (n), Back (b), Exit (e)?"
        match nav.ToLower() with
        | "b" when index > 0 ->
            index <- index - 1
            items.RemoveAt(index)
        | "e" ->
            writeColored ConsoleColor.Red "Inspection cancelled early."
            cancel <- true
        | _ -> index <- index + 1

    if cancel || items.Count = 0 then
        None
    else
        let inspector = prompt "
Inspector name"
        Some {
            Date = DateTime.Now
            Type = checkListType
            Inspector = inspector
            Items = List.ofSeq items
        }

// ====== Save to JSON and CSV File ======
let statusToString = function
    | OK -> "OK"
    | FAIL -> "FAIL"
    | ERROR -> "ERROR"
    | NOT_APPLICABLE -> "N/A"

let saveToFile inspection =
    let dir = "inspections"
    if not (Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore
    let fileBase = Path.Combine(dir, $"inspection_{inspection.Type}_{inspection.Date:yyyyMMdd_HHmm}")

    let jsonSettings = JsonSerializerSettings()
    jsonSettings.Converters.Add(StringEnumConverter()) // Enumokat szövegként menti
    jsonSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    jsonSettings.Formatting <- Formatting.Indented

    let json = JsonConvert.SerializeObject(inspection, jsonSettings)
    File.WriteAllText(fileBase + ".json", json)

    let csvLines =
        ["Description,Status,Comment"] @
        (inspection.Items
         |> List.map (fun item ->
            let statusStr = statusToString item.Status
            let commentStr = item.Comment |> Option.defaultValue ""
            $"{item.Description},{statusStr},{commentStr}"
         ))
    File.WriteAllLines(fileBase + ".csv", csvLines)

    writeColored ConsoleColor.Green ($"\n✔ Successfully saved: {fileBase}.json and .csv")

// ====== List Previous Files by Type ======
let listInspectionsByType typ =
    let dir = "inspections"
    if Directory.Exists(dir) then
        Directory.GetFiles(dir, "*.json")
        |> Array.filter (fun f -> f.Contains($"inspection_{typ}_"))
        |> Array.iter (fun f -> printfn " - %s" (Path.GetFileName(f)))
    else
        writeColored ConsoleColor.Red "No saved inspections directory."

// ====== Load and Display Previous Inspection ======
let loadPreviousInspection () =
    let dir = "inspections"
    if not (Directory.Exists(dir)) then
        writeColored ConsoleColor.Red "No inspection folder found."
    else
        let files = Directory.GetFiles(dir, "*.json") |> Array.sortDescending
        if files.Length = 0 then
            writeColored ConsoleColor.Red "No saved inspections."
        else
            printfn "Available inspections:"
            files |> Array.iteri (fun i file -> printfn "%d. %s" (i+1) (Path.GetFileName(file)))
            let indexStr = prompt "Select file number to load"
            match System.Int32.TryParse(indexStr) with
            | true, index when index > 0 && index <= files.Length ->
                let json = File.ReadAllText(files.[index - 1])
                let jsonSettings = JsonSerializerSettings()
                jsonSettings.Converters.Add(StringEnumConverter())
                jsonSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
                let loaded = JsonConvert.DeserializeObject<Inspection>(json, jsonSettings)
                summarize loaded
                listResults loaded
            | _ ->
                writeColored ConsoleColor.Red "Invalid selection."

// ====== Main Menu ======
let rec menu () =
    drawSeparator()
    writeColored ConsoleColor.White "VEZCHECK – Digital Inspection Checklist"
    printfn "1. New Daily Inspection"
    printfn "2. New Weekly Inspection"
    printfn "3. New Monthly Inspection"
    printfn "4. View Previous Inspection"
    printfn "5. List All Daily Inspections"
    printfn "6. List All Weekly Inspections"
    printfn "7. List All Monthly Inspections"
    printfn "8. Exit"
    drawSeparator()
    let choice = prompt "Select an option"
    match choice with
    | "1" ->
        match runInspection "daily" with
        | Some result ->
            summarize result
            saveToFile result
        | None -> writeColored ConsoleColor.Yellow "Inspection was not completed."
        menu()
    | "2" ->
        match runInspection "weekly" with
        | Some result ->
            summarize result
            saveToFile result
        | None -> writeColored ConsoleColor.Yellow "Inspection was not completed."
        menu()
    | "3" ->
        match runInspection "monthly" with
        | Some result ->
            summarize result
            saveToFile result
        | None -> writeColored ConsoleColor.Yellow "Inspection was not completed."
        menu()
    | "4" ->
        loadPreviousInspection()
        menu()
    | "5" ->
        writeColored ConsoleColor.Cyan "📂 Daily Inspections:"
        listInspectionsByType "daily"
        menu()
    | "6" ->
        writeColored ConsoleColor.Cyan "📂 Weekly Inspections:"
        listInspectionsByType "weekly"
        menu()
    | "7" ->
        writeColored ConsoleColor.Cyan "📂 Monthly Inspections:"
        listInspectionsByType "monthly"
        menu()
    | "8" -> writeColored ConsoleColor.DarkYellow "Exiting..."
    | _ ->
        writeColored ConsoleColor.Red "Invalid option. Try again."
        menu()

[<EntryPoint>]
let main argv =
    menu()
    Console.ReadKey() |> ignore
    0
