<template>
	<div>
		<TemperatureChart :readings="readings" />
	</div>
</template>

<script setup lang="ts">
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { ref } from 'vue'
import TemperatureChart from './components/TemperatureChart.vue'

// Define back-end response
type WeatherData = {
	deviceId: string
	temperature: number
	humidity: number
	timestamp: Date
}

const readings = ref<WeatherData[]>([])

// Also hardcoded ;)
const apiBaseUrl = 'http://localhost:5199'

// setup connection with the hubConnectionBuilder
const connection = new HubConnectionBuilder()
	.withUrl(`${apiBaseUrl}/weather-hub`)
	.configureLogging(LogLevel.Information)
	.build()

// start the connection with the hub
async function start() {
	try {
		await connection.start()
		connection.on('weatherDataProcessed', (payload: WeatherData) => {
			if (!payload.timestamp) payload.timestamp = new Date()
			readings.value.push(payload)
		})
	} catch (err) {
		console.log(err)
		setTimeout(start, 5000)
	}
}

connection.onclose(async () => {
	await start()
})

start()
</script>

<style>
.app-container {
	height: 100vh;
	width: 100vw;
	padding: 1.5rem;
	box-sizing: border-box;
	background: #fafafa;
	display: flex;
	flex-direction: column;
}
</style>
