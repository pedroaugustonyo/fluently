import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class PomodoroPreferencesStore {
  static const _focusMinutesKey = 'pomodoro_focus_minutes';
  static const _shortBreakMinutesKey = 'pomodoro_short_break_minutes';
  static const _longBreakMinutesKey = 'pomodoro_long_break_minutes';
  static const _sessionsPerCycleKey = 'pomodoro_sessions_per_cycle';
  static const _storage = FlutterSecureStorage();

  Future<PomodoroPreferences> read() async {
    final values = await Future.wait([
      _storage.read(key: _focusMinutesKey),
      _storage.read(key: _shortBreakMinutesKey),
      _storage.read(key: _longBreakMinutesKey),
      _storage.read(key: _sessionsPerCycleKey),
    ]);

    return PomodoroPreferences(
      focusMinutes: _readInRange(values[0], 5, 90, 25),
      shortBreakMinutes: _readInRange(values[1], 1, 30, 5),
      longBreakMinutes: _readInRange(values[2], 5, 60, 15),
      sessionsPerCycle: _readInRange(values[3], 2, 8, 4),
    );
  }

  Future<void> save(PomodoroPreferences preferences) => Future.wait([
    _storage.write(
      key: _focusMinutesKey,
      value: preferences.focusMinutes.toString(),
    ),
    _storage.write(
      key: _shortBreakMinutesKey,
      value: preferences.shortBreakMinutes.toString(),
    ),
    _storage.write(
      key: _longBreakMinutesKey,
      value: preferences.longBreakMinutes.toString(),
    ),
    _storage.write(
      key: _sessionsPerCycleKey,
      value: preferences.sessionsPerCycle.toString(),
    ),
  ]).then((_) {});

  int _readInRange(String? value, int min, int max, int fallback) {
    final parsed = int.tryParse(value ?? '');
    return parsed != null && parsed >= min && parsed <= max ? parsed : fallback;
  }
}

class PomodoroPreferences {
  const PomodoroPreferences({
    required this.focusMinutes,
    required this.shortBreakMinutes,
    required this.longBreakMinutes,
    required this.sessionsPerCycle,
  });

  final int focusMinutes;
  final int shortBreakMinutes;
  final int longBreakMinutes;
  final int sessionsPerCycle;
}
