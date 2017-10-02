namespace WinTail

[<AutoOpen>]
module Messages =
    type Command = 
    | Start
    | Continue
    | Message of string
    | Exit

    type ErrorType =
    | Null
    | Validation

    type InputResult =
    | InputSuccess of string
    | InputError of reason: string * errorType: ErrorType