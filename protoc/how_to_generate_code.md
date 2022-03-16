## For C code
protoc -I=. --c_out=../server_programs/protoc_generated ./game.proto
<!-- protoc -I=. --cpp_out=../server_programs/protoc_generated ./game.proto -->

## For C# code
protoc -I=. --csharp_out=../unity/protoc_generated ./game.proto