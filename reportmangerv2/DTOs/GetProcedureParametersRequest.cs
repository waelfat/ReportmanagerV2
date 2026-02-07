using System;

namespace reportmangerv2.DTOs;

public class GetProcedureParametersRequest
{
     public required string ProcedureName { get; set; }
    public required string SchemaId { get; set; }

}
