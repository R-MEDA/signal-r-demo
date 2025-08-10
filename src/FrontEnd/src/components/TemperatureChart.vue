<script setup lang="ts">
import { computed, ref } from 'vue'
import { Line } from 'vue-chartjs'
import {
	Chart,
	LineController,
	LineElement,
	PointElement,
	LinearScale,
	Title,
	CategoryScale,
	TimeScale,
	Tooltip,
	Legend,
	Filler,
	ArcElement,
	type ChartOptions,
} from 'chart.js'
import 'chartjs-adapter-date-fns'

Chart.register(
	LineController,
	LineElement,
	PointElement,
	LinearScale,
	Title,
	CategoryScale,
	TimeScale,
	Tooltip,
	Legend,
	Filler,
	ArcElement,
)

const selectedTab = ref<'temp' | 'hum'>('temp')

const props = defineProps<{
	readings: {
		deviceId: string
		temperature: number
		humidity: number
		timestamp: Date
	}[]
}>()

const devices = computed(() => Array.from(new Set(props.readings.map((r) => r.deviceId))))

const sortedReadings = computed(() =>
	[...props.readings]
		.map((r) => ({ ...r, timestamp: new Date(r.timestamp) }))
		.sort((a, b) => a.timestamp.getTime() - b.timestamp.getTime()),
)

function latestReading(deviceId: string) {
	for (let i = sortedReadings.value.length - 1; i >= 0; i--) {
		if (sortedReadings.value[i].deviceId === deviceId) return sortedReadings.value[i]
	}
	return null
}

const lastUpdate = computed(() => {
	if (!sortedReadings.value.length) return null
	return sortedReadings.value[sortedReadings.value.length - 1].timestamp
})

const baseColors = [
	'255, 99, 132',
	'54, 162, 235',
	'255, 206, 86',
	'75, 192, 192',
	'153, 102, 255',
	'255, 159, 64',
]

function getColor(deviceIndex: number, alpha = 1) {
	const c = baseColors[deviceIndex % baseColors.length]
	return `rgba(${c},${alpha})`
}

type DeviceStatus = 'Normal' | 'Warning' | 'Critical'

function deviceStatus(reading: { temperature: number; humidity: number }): DeviceStatus {
	if (reading.temperature > 38 || reading.humidity > 55) return 'Critical'
	if (reading.temperature > 35 || reading.humidity > 50) return 'Warning'
	return 'Normal'
}

const deviceStatuses = computed(() => {
	const statuses: Record<string, DeviceStatus> = {}
	devices.value.forEach((device) => {
		const last = latestReading(device)
		statuses[device] = last ? deviceStatus(last) : 'Normal'
	})
	return statuses
})

const chartData = computed(() => {
	const datasets: any[] = []
	const ctx = document.createElement('canvas').getContext('2d')
	if (!ctx) return { datasets }

	devices.value.forEach((device, idx) => {
		const deviceData = sortedReadings.value.filter((r) => r.deviceId === device)

		const tempGradient = ctx.createLinearGradient(0, 0, 0, 400)
		tempGradient.addColorStop(0, getColor(idx, 0.4))
		tempGradient.addColorStop(1, getColor(idx, 0))

		const humGradient = ctx.createLinearGradient(0, 0, 0, 400)
		humGradient.addColorStop(0, getColor(idx + baseColors.length / 2, 0.4))
		humGradient.addColorStop(1, getColor(idx + baseColors.length / 2, 0))

		if (selectedTab.value === 'temp') {
			datasets.push({
				label: `Temperature (${device})`,
				data: deviceData.map((r) => ({ x: r.timestamp, y: r.temperature })),
				borderColor: getColor(idx),
				backgroundColor: tempGradient,
				fill: true,
				tension: 0.4,
				yAxisID: 'yTemp',
				pointRadius: 2,
				pointHoverRadius: 7,
				borderWidth: 2,
			})
		} else if (selectedTab.value === 'hum') {
			datasets.push({
				label: `Humidity (${device})`,
				data: deviceData.map((r) => ({ x: r.timestamp, y: r.humidity })),
				borderColor: getColor(idx + baseColors.length / 2),
				backgroundColor: humGradient,
				fill: true,
				tension: 0.4,
				yAxisID: 'yHum',
				pointRadius: 4,
				pointHoverRadius: 7,
				borderWidth: 2,
			})
		}
	})

	return { datasets }
})

const chartOptions = computed<ChartOptions<'line'>>(() => ({
	responsive: true,
	maintainAspectRatio: false,
	interaction: { mode: 'nearest', intersect: false },
	scales: {
		x: {
			type: 'time',
			time: {
				unit: 'second',
				tooltipFormat: 'PPpp',
				displayFormats: { second: 'HH:mm:ss', minute: 'HH:mm' },
			},
			title: {
				display: true,
				text: 'Timestamp',
				color: '#555',
				font: { weight: 'bold' },
			},
			grid: { color: 'rgba(200, 200, 200, 0.2)' },
			ticks: { maxRotation: 45, autoSkip: false, color: '#666' },
		},
		...(selectedTab.value === 'temp'
			? {
					yTemp: {
						type: 'linear',
						position: 'left',
						min: 28,
						max: 42,
						title: {
							display: true,
							text: 'Temperature (°C)',
							color: '#555',
							font: { weight: 'bold' },
						},
						ticks: { stepSize: 2, color: '#666' },
						grid: { color: 'rgba(200, 200, 200, 0.2)' },
					},
				}
			: {
					yHum: {
						type: 'linear',
						position: 'left',
						min: 40,
						max: 60,
						title: {
							display: true,
							text: 'Humidity (%)',
							color: '#555',
							font: { weight: 'bold' },
						},
						ticks: { stepSize: 5, color: '#666' },
						grid: { color: 'rgba(200, 200, 200, 0.2)' },
					},
				}),
	},
	plugins: {
		legend: {
			display: true,
			position: 'top',
			labels: { color: '#444', font: { size: 12, weight: 'bold' } },
		},
		tooltip: {
			mode: 'index',
			intersect: false,
			backgroundColor: 'rgba(0,0,0,0.8)',
			titleFont: { weight: 'bold' },
			bodyFont: { size: 12 },
			padding: 10,
			callbacks: {
				label(ctx: any) {
					return `${ctx.dataset.label}: ${ctx.parsed.y.toFixed(1)}`
				},
			},
		},
	},
}))

const sparklineOptions = {
	responsive: true,
	maintainAspectRatio: false,
	scales: { x: { display: false }, y: { display: false } },
	elements: { line: { borderWidth: 2, tension: 0.3, fill: false }, point: { radius: 0 } },
	plugins: { legend: { display: false }, tooltip: { enabled: false } },
}
</script>

<template>
	<div class="temperature-chart">
		<header class="chart-header">
			<h2>Device Overview</h2>
			<div class="last-update">
				<span v-if="lastUpdate" style="font-weight: bold"
					>Last updated: {{ new Date(lastUpdate).toLocaleString() }}</span
				>
			</div>
		</header>

		<section class="device-summary">
			<article v-for="(device, idx) in devices" :key="device" class="device-card">
				<div class="device-header">
					<h3>{{ device }}</h3>
					<span
						class="status-badge"
						:class="deviceStatuses[device].toLowerCase()"
						:title="deviceStatuses[device]"
					>
						{{ deviceStatuses[device] }}
					</span>
				</div>

				<div><strong>Latest Reading:</strong></div>
				<div>Temp: {{ latestReading(device)?.temperature ?? 'N/A' }} °C</div>
				<div>Humidity: {{ latestReading(device)?.humidity ?? 'N/A' }} %</div>

				<div class="sparkline-chart">
					<Line
						:data="{
							labels: sortedReadings
								.filter((r) => r.deviceId === device)
								.map((r) => new Date(r.timestamp)),
							datasets: [
								{
									data: sortedReadings
										.filter((r) => r.deviceId === device)
										.map((r) =>
											selectedTab === 'temp' ? r.temperature : r.humidity,
										),
									borderColor: getColor(idx),
									borderWidth: 2,
									fill: false,
									tension: 0.4,
									pointRadius: 0,
								},
							],
						}"
						:options="sparklineOptions"
						height="70"
					/>
				</div>
			</article>
		</section>

		<nav class="tab-bar">
			<button :class="{ active: selectedTab === 'temp' }" @click="selectedTab = 'temp'">
				Temperature
			</button>
			<button :class="{ active: selectedTab === 'hum' }" @click="selectedTab = 'hum'">
				Humidity
			</button>
		</nav>

		<section class="chart-container">
			<Line :data="chartData" :options="chartOptions" />
		</section>
	</div>
</template>

<style scoped lang="scss">
.temperature-chart {
	padding: 1rem 1rem;
	font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
	color: #222;
}

.chart-header {
	display: flex;
	justify-content: space-between;
	align-items: baseline;
	margin-bottom: 1.5rem;

	h2 {
		font-size: 1.8rem;
		font-weight: 700;
		color: #333;
	}

	.last-update {
		font-size: 0.9rem;
		color: #666;
		font-weight: 600;
	}
}

.global-summary {
	display: flex;
	flex-wrap: wrap;
	gap: 1.5rem;
	padding-bottom: 1.5rem;

	.summary-card {
		background: #f8f9fa;
		border-radius: 8px;
		box-shadow: 0 2px 5px rgb(0 0 0 / 0.05);
		padding: 1.5rem;
		flex: 1 1 180px;
		min-width: 180px;
		color: #444;
		text-align: center;
		font-weight: 600;
		font-size: 1.1rem;

		h3 {
			margin-bottom: 0.5rem;
			color: #333;
			font-weight: 700;
			font-size: 1.15rem;
		}

		p {
			font-size: 1.25rem;
			margin: 0;
			font-weight: 700;
		}

		small {
			display: block;
			margin-top: 0.25rem;
			font-size: 0.85rem;
			color: #666;
			font-weight: 500;
		}
	}
}

.status-distribution {
	max-width: 200px;
	margin-left: auto;
	margin-top: 1rem;
	padding: 0.5rem 0;

	h4 {
		font-weight: 600;
		font-size: 1rem;
		color: #666;
		margin-bottom: 0.6rem;
		text-align: right;
	}
}

.device-summary {
	display: flex;
	flex-wrap: wrap;
	gap: 1.25rem;
	padding: 1rem 1rem 1rem 1rem;
	justify-content: center;

	.device-card {
		background: #ffffff;
		border: 1px solid #ddd;
		border-radius: 10px;
		box-shadow: 0 3px 8px rgb(0 0 0 / 0.07);
		padding: 1.2rem 1.5rem;
		min-width: 260px;
		flex: 1 1 260px;
		display: flex;
		flex-direction: column;
		color: #333;

		.device-header {
			display: flex;
			justify-content: space-between;
			align-items: center;
			margin-bottom: 0.8rem;

			h3 {
				font-size: 1.2rem;
				font-weight: 700;
				margin: 0;
			}

			.status-badge {
				padding: 0.35rem 0.8rem;
				border-radius: 15px;
				font-weight: 700;
				font-size: 0.85rem;
				color: white;
				user-select: none;
			}

			.status-badge.normal {
				background-color: #28a745;
			}

			.status-badge.warning {
				background-color: #ffc107;
				color: #3a3a3a;
			}

			.status-badge.critical {
				background-color: #dc3545;
			}
		}

		> div {
			font-weight: 600;
			margin-bottom: 0.3rem;
			font-size: 0.95rem;
		}

		.sparkline-chart {
			height: 70px;
			margin-top: 0.6rem;
			user-select: none;
		}
	}
}

.tab-bar {
	display: flex;
	justify-content: center;
	gap: 1rem;
	margin: 1.25rem 0;

	button {
		background: #eee;
		border: none;
		border-radius: 25px;
		padding: 0.6rem 1.8rem;
		font-weight: 700;
		font-size: 1rem;
		cursor: pointer;
		transition:
			background-color 0.25s ease,
			color 0.25s ease;
		box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
		color: #555;

		&:hover {
			background: #ddd;
		}

		&.active {
			background: #007bff;
			color: white;
			box-shadow: 0 4px 10px rgba(0, 123, 255, 0.4);
		}
	}
}

.chart-container {
	min-height: 400px;
	padding: 1rem 1rem;
	margin-bottom: 2rem;
}
</style>
