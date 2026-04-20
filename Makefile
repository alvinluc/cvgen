cv:
	docker compose run app dotnet run --project Application $(File) $(Format)

cover-letter:
	docker compose run app dotnet run --project Application $(File) $(Format) --cover-letter
