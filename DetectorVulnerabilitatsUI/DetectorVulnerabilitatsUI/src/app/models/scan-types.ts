export const SCAN_TYPES = [
  {
    id: 0,
    name: 'Services',
    description: 'Detecta ports, versions exactes dels serveis i busca CVEs coneguts per a aquestes versions.',
    iconPath: 'M4 6h16M4 12h16M4 18h16'
  },
  {
    id: 1,
    name: 'Infrastructure',
    description: 'Ataca directament protocols de xarxa i bases de dades mal configurades (no web).',
    iconPath: 'M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71'
  },
  {
    id: 2,
    name: 'Web Enumeration',
    description: `Troba panells d'administració, tecnologies utilitzades i fitxers exposats, però sense llançar exploits.`,
    iconPath: 'M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'
  },
  {
    id: 3,
    name: 'Web Vulnerabilities',
    description: 'Escaneig agressiu contra aplicacions web. Prova injeccions (XSS, SQLi) i execució de codi (RCE).',
    iconPath: 'M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z'
  },
  {
    id: 4,
    name: 'Database',
    description: 'Escaneig específic per auditar la seguretat de bases de dades (MySQL, Mongo, MSSQL).',
    iconPath: 'M12 3c-4.97 0-9 1.34-9 3s4.03 3 9 3 9-1.34 9-3-4.03-3-9-3zM3 6v13c0 1.66 4.03 3 9 3s9-1.34 9-3V6'
  }
];
