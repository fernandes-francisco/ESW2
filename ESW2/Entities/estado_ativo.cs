using NpgsqlTypes; 
namespace ESW2.Entities
 {
     public enum estado_ativo
     {
         [PgName("Ativo")]  
         Ativo,

         [PgName("Encerrado")] 
         Encerrado,

         [PgName("Em_Periodo")] 
         Em_Periodo
     }
 }