module RopResult

type Result<'TSuccess, 'TMessage> =
    | Success of 'TSuccess
    | Failure of 'TMessage list

let onSuccess success = function
    | Success x -> success x
    | Failure messages -> Failure messages

let convert convert result = 
    onSuccess (fun x -> Success (convert x)) result

let mergeResults mergeValues a b  =
    match a, b with
    | (Success a), (Success b) -> Success (mergeValues a b)
    | Success _, Failure messages -> Failure messages
    | Failure messages, Success _ -> Failure messages
    | Failure a, Failure b -> Failure (b @ a)


let foldResultList<'a, 'b> : (Result<'a, 'b> list -> Result<'a list, 'b>) = 
    let mergeValues (aList: 'a list) (bValue: 'a) =
        bValue::aList
    List.fold (mergeResults mergeValues) (Success [])

let mergeListResults a b =
    mergeResults (fun aList bList -> aList @ bList) a b