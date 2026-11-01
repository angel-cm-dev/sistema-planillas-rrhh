import { formatCurrency, formatDate, formatPeriodo, pageRange, parseHorasExtra } from './format';

describe('format', () => {
  it('formatea montos en soles con dos decimales', () => {
    const texto = formatCurrency(1234.5);
    expect(texto).toContain('S/');
    expect(texto).toMatch(/1[,.]?234\.50/);
  });

  it('trata null/undefined como cero', () => {
    expect(formatCurrency(undefined)).toContain('0.00');
  });

  it('formatea periodos "AAAA-MM" en texto legible', () => {
    expect(formatPeriodo('2026-10')).toBe('Octubre 2026');
    expect(formatPeriodo('2026-13')).toBe('2026-13');
  });

  it('formatea fechas ISO sin desfase de zona horaria', () => {
    expect(formatDate('2026-01-01')).toBe('01/01/2026');
    expect(formatDate('2026-10-05T00:00:00Z')).toBe('05/10/2026');
    expect(formatDate(null)).toBe('—');
  });

  it('interpreta horas extra separadas por coma o espacio', () => {
    expect(parseHorasExtra('2, 3 1.5')).toEqual([2, 3, 1.5]);
    expect(parseHorasExtra('abc, -1, 0')).toEqual([]);
  });
});

describe('pageRange', () => {
  it('muestra todas las páginas si son pocas', () => {
    expect(pageRange(2, 3)).toEqual([1, 2, 3]);
  });

  it('inserta elipsis en los huecos', () => {
    expect(pageRange(5, 10)).toEqual([1, '…', 4, 5, 6, '…', 10]);
  });

  it('devuelve [1] cuando hay una sola página', () => {
    expect(pageRange(1, 1)).toEqual([1]);
  });
});
