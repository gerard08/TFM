// --- Dades Mock Inicials (Només es fan servir si no hi ha res guardat) ---
export const INITIAL_STATS = {
  scansTotal: 128,
  vulnsHigh: 3,
  uniqueTargets: 12,
  lastScan: 'Avui, 10:42 AM',
  status: 'Operatiu'
};

export const INITIAL_LOGS = [
  { id: 1, target: '192.168.1.15', date: '2023-10-27 14:30', status: 'Net', duration: '15m 10s' },
  { id: 2, target: 'prod-server-01', date: '2023-10-27 10:15', status: '3 Riscos', duration: '22m 45s' },
  { id: 3, target: 'test-env-db', date: '2023-10-26 18:20', status: 'Net', duration: '12m 20s' },
];

export const INITIAL_HISTORY = [
  { id: 101, target: '192.168.1.15', date: '2023-11-20 14:30', status: 'Net', findings: 0, duration: '15m 10s' },
  { id: 102, target: 'prod-api-v2', date: '2023-11-20 09:15', status: 'Crític', findings: 5, duration: '45m 12s' },
  { id: 103, target: 'dev-server-alpha', date: '2023-11-19 16:20', status: 'Advertència', findings: 2, duration: '14m 20s' },
  { id: 104, target: '192.168.1.200', date: '2023-11-18 11:00', status: 'Net', findings: 0, duration: '10m 55s' },
  { id: 105, target: 'web-legacy-01', date: '2023-11-15 10:30', status: 'Alt Risc', findings: 8, duration: '28m 15s' },
  { id: 106, target: 'test-env-db', date: '2023-11-14 15:45', status: 'Net', findings: 0, duration: '13m 10s' },
];
