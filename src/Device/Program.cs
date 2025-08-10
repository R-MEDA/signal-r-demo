using System.Text;
using System.Text.Json;

Console.WriteLine("Weather data generator starting…");

// Hard coded for demo purposes ;)
var apiBaseUrl = new Uri("http://localhost:5199");

using HttpClient client = new HttpClient { BaseAddress = apiBaseUrl };

// Define 5 devices (cities)
var cities = new[]
{
	new CityDevice("New York", 35.0, 50.0, 30.0, 40.0, 45.0, 55.0),
	new CityDevice("London",   35.0, 50.0, 28.0, 42.0, 40.0, 60.0),
	new CityDevice("Tokyo",    35.0, 50.0, 29.0, 41.0, 43.0, 57.0),
	new CityDevice("Sydney",   35.0, 50.0, 30.0, 40.0, 45.0, 55.0),
	new CityDevice("Paris",    35.0, 50.0, 30.0, 40.0, 45.0, 55.0)
};

while (true)
{
	foreach (var city in cities)
	{
		var reading = city.GenerateReading();

		string json = JsonSerializer.Serialize(reading);
		StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

		try
		{
			using HttpResponseMessage response = await client.PostAsync("/weather-reading", content);
			Console.WriteLine($"Sent by {city.Name}: {json}, Response: {response.StatusCode}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Errror occured: {ex.Message}");
		}
	}

	await Task.Delay(1000);
}

public record WeatherData(string DeviceId, int Temperature, int Humidity);

public class CityDevice(string name, double startTemp, double startHum,
	double tempMin, double tempMax, double humMin, double humMax)
{
	public string Name { get; } = name;
	public double Temperature { get; set; } = startTemp;
	public double Humidity { get; set; } = startHum;
	public double TempMin { get; } = tempMin;
	public double TempMax { get; } = tempMax;
	public double HumMin { get; } = humMin;
	public double HumMax { get; } = humMax;
	private static Random _random = new Random();

	public WeatherData GenerateReading()
	{
		// Temperature change
		Temperature += (_random.NextDouble() * 5.0) - 2.5;
		Temperature = Math.Clamp(Temperature, TempMin, TempMax);

		// Humidity change
		Humidity += (_random.NextDouble() * 10.0) - 5.0;
		Humidity = Math.Clamp(Humidity, HumMin, HumMax);

		return new WeatherData(
			DeviceId: 			Name,
			Temperature: 		(int)Math.Round(Temperature),
			Humidity: 			(int)Math.Round(Humidity)
		);
	}
}
