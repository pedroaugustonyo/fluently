import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';

import 'confetti_animation.dart';
import 'fluently_api_client.dart';
import 'pomodoro_preferences_store.dart';

const _primary = Color(0xFF00A887);
const _ink = Color(0xFF10203C);
const _muted = Color(0xFF71819C);
const _surface = Color(0xFFF1F8F6);
const _outline = Color(0xFFBFEDE4);

class TasksPage extends StatefulWidget {
  const TasksPage({super.key, required this.api, required this.onApiError});

  final FluentlyApiClient api;
  final bool Function(ApiException error) onApiError;

  @override
  State<TasksPage> createState() => _TasksPageState();
}

class _TasksPageState extends State<TasksPage>
    with SingleTickerProviderStateMixin {
  List<TodoTask> _tasks = const [];
  final _searchController = TextEditingController();
  DailyXpGoal? _goal;
  Timer? _searchDebounce;
  int _page = 1;
  bool _hasMore = false;
  bool _loading = true;
  bool _loadingMore = false;
  bool _celebratedGoal = false;
  final _goalCardLink = LayerLink();
  late final AnimationController _confettiController;

  @override
  void initState() {
    super.initState();
    _confettiController = AnimationController(
      vsync: this,
      duration: confettiAnimationDuration,
    );
    _load();
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    _confettiController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final values = await Future.wait([
        widget.api.getDailyXpGoal(),
        widget.api.getTasks(search: _searchController.text),
      ]);
      if (!mounted) return;
      setState(() {
        _goal = values[0] as DailyXpGoal;
        final taskPage = values[1] as TaskPage;
        _tasks = taskPage.items..sort(_taskOrder);
        _page = taskPage.page;
        _hasMore = taskPage.hasMore;
      });
      _celebrateGoalIfNeeded();
    } on ApiException catch (error) {
      _showError(error);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _celebrateGoalIfNeeded() {
    if (_celebratedGoal || _goal?.isCompleted != true) {
      return;
    }

    _celebratedGoal = true;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        _confettiController.forward(from: 0);
      }
    });
  }

  void _search(String value) {
    _searchDebounce?.cancel();
    _searchDebounce = Timer(
      const Duration(milliseconds: 300),
      () => _loadTasks(search: value),
    );
  }

  Future<void> _loadTasks({String? search, bool loadMore = false}) async {
    if (loadMore && (_loadingMore || !_hasMore)) {
      return;
    }

    if (loadMore && mounted) {
      setState(() => _loadingMore = true);
    }

    try {
      final taskPage = await widget.api.getTasks(
        page: loadMore ? _page + 1 : 1,
        search: search ?? _searchController.text,
      );
      if (mounted) {
        setState(() {
          _tasks = loadMore ? [..._tasks, ...taskPage.items] : taskPage.items;
          _tasks.sort(_taskOrder);
          _page = taskPage.page;
          _hasMore = taskPage.hasMore;
        });
      }
    } on ApiException catch (error) {
      _showError(error);
    } finally {
      if (loadMore && mounted) {
        setState(() => _loadingMore = false);
      }
    }
  }

  void _showError(ApiException error) {
    if (!widget.onApiError(error) && mounted) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(error.message)));
    }
  }

  Future<void> _showGoalSheet() async {
    var target = (_goal?.targetXp ?? 50).clamp(10, 1000).toDouble();
    await showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => StatefulBuilder(
        builder: (context, updateSheet) => _SheetFrame(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Meta diária de XP', style: _titleStyle),
              const SizedBox(height: 8),
              const Text(
                'Escolha um ritmo que faça seu inglês avançar todos os dias.',
                style: TextStyle(color: _muted, height: 1.35),
              ),
              const SizedBox(height: 26),
              Center(
                child: TweenAnimationBuilder<double>(
                  tween: Tween(end: target),
                  duration: const Duration(milliseconds: 280),
                  curve: Curves.easeOutCubic,
                  builder: (context, value, _) => Text(
                    '${value.round()} XP',
                    style: const TextStyle(
                      color: _primary,
                      fontSize: 38,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ),
              SliderTheme(
                data: SliderTheme.of(context).copyWith(
                  activeTrackColor: _primary,
                  inactiveTrackColor: _outline,
                  thumbColor: Colors.white,
                  overlayColor: _primary.withValues(alpha: .12),
                  thumbShape: const RoundSliderThumbShape(
                    enabledThumbRadius: 12,
                  ),
                  trackHeight: 7,
                ),
                child: Slider(
                  min: 10,
                  max: 1000,
                  divisions: 99,
                  value: target,
                  onChanged: (value) => updateSheet(() => target = value),
                ),
              ),
              const Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('10 XP', style: TextStyle(color: _muted, fontSize: 12)),
                  Text(
                    '1000 XP',
                    style: TextStyle(color: _muted, fontSize: 12),
                  ),
                ],
              ),
              const SizedBox(height: 26),
              _PrimaryButton(
                label: 'Salvar meta',
                onPressed: () async {
                  try {
                    final goal = await widget.api.updateDailyXpGoal(
                      target.round(),
                    );
                    if (mounted) {
                      setState(() {
                        _goal = goal;
                        if (goal.isCompleted) {
                          _celebratedGoal = false;
                        }
                      });
                      _celebrateGoalIfNeeded();
                    }
                    if (context.mounted) Navigator.pop(context);
                  } on ApiException catch (error) {
                    _showError(error);
                  }
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _showTaskSheet([TodoTask? task]) async {
    final title = TextEditingController(text: task?.title ?? '');
    final formKey = GlobalKey<FormState>();
    var priority = task?.priority ?? 2;
    var dueDate = task?.dueDate == null
        ? null
        : DateTime.tryParse(task!.dueDate!);
    await showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      isScrollControlled: true,
      builder: (context) => StatefulBuilder(
        builder: (context, updateSheet) => _SheetFrame(
          child: Form(
            key: formKey,
            child: Padding(
              padding: EdgeInsets.only(
                bottom: MediaQuery.viewInsetsOf(context).bottom,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        task == null ? 'Nova tarefa' : 'Editar tarefa',
                        style: _titleStyle,
                      ),
                      const Spacer(),
                      IconButton(
                        onPressed: () => Navigator.pop(context),
                        icon: const Icon(Icons.close_rounded, color: _muted),
                      ),
                    ],
                  ),
                  const SizedBox(height: 14),
                  TextFormField(
                    controller: title,
                    autofocus: true,
                    textCapitalization: TextCapitalization.sentences,
                    maxLength: 160,
                    autovalidateMode: AutovalidateMode.onUserInteraction,
                    validator: _taskTitleValidator,
                    decoration: const InputDecoration(
                      hintText: 'O que você quer concluir?',
                      filled: true,
                      fillColor: _surface,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.all(Radius.circular(16)),
                        borderSide: BorderSide(color: _outline),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.all(Radius.circular(16)),
                        borderSide: BorderSide(color: _outline),
                      ),
                    ),
                  ),
                  const SizedBox(height: 22),
                  const Text(
                    'Prioridade',
                    style: TextStyle(color: _ink, fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      for (final value in [1, 2, 3])
                        Expanded(
                          child: Padding(
                            padding: EdgeInsets.only(right: value == 3 ? 0 : 8),
                            child: _PriorityChoice(
                              value: value,
                              selected: priority == value,
                              onTap: () => updateSheet(() => priority = value),
                            ),
                          ),
                        ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  _SelectorTile(
                    icon: Icons.calendar_month_rounded,
                    label: dueDate == null
                        ? 'Sem data de entrega'
                        : _dateLabel(dueDate!),
                    onTap: () async {
                      final selected = await showDatePicker(
                        context: context,
                        initialDate: dueDate ?? DateTime.now(),
                        firstDate: DateTime.now().subtract(
                          const Duration(days: 1),
                        ),
                        lastDate: DateTime(2100),
                      );
                      if (selected != null) {
                        updateSheet(() => dueDate = selected);
                      }
                    },
                  ),
                  if (dueDate != null)
                    TextButton.icon(
                      onPressed: () => updateSheet(() => dueDate = null),
                      icon: const Icon(Icons.close_rounded, size: 16),
                      label: const Text('Remover data'),
                    ),
                  const SizedBox(height: 12),
                  _PrimaryButton(
                    label: task == null ? 'Criar tarefa' : 'Salvar alterações',
                    onPressed: () async {
                      if (!formKey.currentState!.validate()) {
                        return;
                      }
                      try {
                        if (task == null) {
                          await widget.api.createTask(
                            title: title.text.trim(),
                            priority: priority,
                            dueDate: dueDate == null
                                ? null
                                : _apiDate(dueDate!),
                          );
                        } else {
                          await widget.api.updateTask(
                            id: task.id,
                            title: title.text.trim(),
                            priority: priority,
                            dueDate: dueDate == null
                                ? null
                                : _apiDate(dueDate!),
                          );
                        }
                        await _loadTasks();
                        if (context.mounted) {
                          Navigator.pop(context);
                        }
                      } on ApiException catch (error) {
                        _showError(error);
                      }
                    },
                  ),
                  if (task != null) ...[
                    const SizedBox(height: 8),
                    TextButton(
                      onPressed: () async {
                        Navigator.pop(context);
                        await _confirmDelete(task);
                      },
                      style: TextButton.styleFrom(
                        foregroundColor: const Color(0xFFE95C64),
                      ),
                      child: const Center(child: Text('Excluir tarefa')),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _confirmDelete(TodoTask task) async {
    final delete = await showModalBottomSheet<bool>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => _SheetFrame(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Excluir tarefa?', style: _titleStyle),
            const SizedBox(height: 8),
            Text(
              '“${task.title}” será removida permanentemente.',
              style: const TextStyle(color: _muted),
            ),
            const SizedBox(height: 24),
            _PrimaryButton(
              label: 'Excluir tarefa',
              color: const Color(0xFFE95C64),
              onPressed: () => Navigator.pop(context, true),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Center(child: Text('Cancelar')),
            ),
          ],
        ),
      ),
    );
    if (delete != true) {
      return;
    }
    try {
      await widget.api.deleteTask(task.id);
      await _loadTasks();
    } on ApiException catch (error) {
      _showError(error);
    }
  }

  Future<void> _toggleCompletion(TodoTask task, bool completed) async {
    try {
      await widget.api.setTaskCompletion(task.id, completed);
      await _loadTasks();
    } on ApiException catch (error) {
      _showError(error);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator(color: _primary));
    }
    final pending = _tasks.where((task) => !task.isCompleted).toList();
    final completed = _tasks.where((task) => task.isCompleted).toList();
    final target = _goal?.targetXp ?? 50;
    final progress = target == 0 ? 0.0 : (_goal?.earnedXp ?? 0) / target;
    final completedGoal = _goal?.isCompleted == true;
    return Stack(
      children: [
        RefreshIndicator(
          color: _primary,
          onRefresh: _load,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(22, 18, 22, 30),
            children: [
              Row(
                children: [
                  Image.asset('assets/images/fluently_icon.png', width: 30),
                  const SizedBox(width: 10),
                  const Text(
                    'Tarefas',
                    style: TextStyle(
                      color: _ink,
                      fontSize: 24,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const Spacer(),
                ],
              ),
              const SizedBox(height: 24),
              CompositedTransformTarget(
                link: _goalCardLink,
                child: Stack(
                  children: [
                    InkWell(
                      onTap: _showGoalSheet,
                      borderRadius: BorderRadius.circular(20),
                      child: Container(
                        padding: const EdgeInsets.all(18),
                        decoration: BoxDecoration(
                          color: completedGoal ? null : _surface,
                          gradient: completedGoal
                              ? const LinearGradient(
                                  colors: [
                                    Color(0xFFE2FAF4),
                                    Color(0xFFF4FCF8),
                                  ],
                                  begin: Alignment.topLeft,
                                  end: Alignment.bottomRight,
                                )
                              : null,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(
                            color: completedGoal
                                ? const Color(0xFF72DCC8)
                                : _outline,
                            width: completedGoal ? 1.4 : 1,
                          ),
                          boxShadow: completedGoal
                              ? [
                                  BoxShadow(
                                    color: _primary.withValues(alpha: .12),
                                    blurRadius: 20,
                                    offset: const Offset(0, 8),
                                  ),
                                ]
                              : null,
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Container(
                                  width: 32,
                                  height: 32,
                                  decoration: BoxDecoration(
                                    color: completedGoal
                                        ? _primary
                                        : Colors.transparent,
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(
                                    completedGoal
                                        ? Icons.workspace_premium_rounded
                                        : Icons.bolt_rounded,
                                    color: completedGoal
                                        ? Colors.white
                                        : _primary,
                                    size: 20,
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Text(
                                  completedGoal
                                      ? 'Meta concluída!'
                                      : 'Meta diária',
                                  style: const TextStyle(
                                    color: _primary,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                const Spacer(),
                                Text(
                                  '${_goal?.earnedXp ?? 0}/$target XP',
                                  style: const TextStyle(
                                    color: _muted,
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                const Icon(
                                  Icons.chevron_right_rounded,
                                  color: _muted,
                                ),
                              ],
                            ),
                            const SizedBox(height: 14),
                            TweenAnimationBuilder<double>(
                              tween: Tween(end: progress.clamp(0, 1)),
                              duration: const Duration(milliseconds: 650),
                              curve: Curves.easeOutCubic,
                              builder: (context, value, _) => ClipRRect(
                                borderRadius: BorderRadius.circular(20),
                                child: LinearProgressIndicator(
                                  value: value,
                                  minHeight: 9,
                                  color: _primary,
                                  backgroundColor: Colors.white,
                                ),
                              ),
                            ),
                            const SizedBox(height: 10),
                            Text(
                              completedGoal
                                  ? 'Você cumpriu sua meta de hoje. Continue assim!'
                                  : 'Toque para ajustar sua meta.',
                              style: const TextStyle(
                                color: _muted,
                                fontSize: 13,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 28),
              Row(
                children: [
                  const Text('Suas tarefas', style: _sectionStyle),
                  const Spacer(),
                  _RoundAction(
                    icon: Icons.add_rounded,
                    onTap: () => _showTaskSheet(),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              TextField(
                controller: _searchController,
                onChanged: _search,
                textInputAction: TextInputAction.search,
                decoration: InputDecoration(
                  hintText: 'Buscar tarefa',
                  prefixIcon: const Icon(Icons.search_rounded),
                  suffixIcon: _searchController.text.isEmpty
                      ? null
                      : IconButton(
                          onPressed: () {
                            _searchController.clear();
                            _search('');
                          },
                          icon: const Icon(Icons.close_rounded),
                        ),
                  filled: true,
                  fillColor: _surface,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(16),
                    borderSide: const BorderSide(color: _outline),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(16),
                    borderSide: const BorderSide(color: _outline),
                  ),
                ),
              ),
              const SizedBox(height: 18),
              if (_tasks.isEmpty)
                _EmptyTasks(onAdd: () => _showTaskSheet())
              else if (pending.isNotEmpty)
                ...pending.map(
                  (task) => _TaskCard(
                    task: task,
                    onToggle: (value) => _toggleCompletion(task, value),
                    onEdit: () => _showTaskSheet(task),
                    onDelete: () => _confirmDelete(task),
                  ),
                ),
              if (completed.isNotEmpty) ...[
                const SizedBox(height: 22),
                Text(
                  'Concluídas (${completed.length})',
                  style: _sectionStyle.copyWith(fontSize: 17),
                ),
                const SizedBox(height: 10),
                ...completed.map(
                  (task) => _TaskCard(
                    task: task,
                    onToggle: (value) => _toggleCompletion(task, value),
                    onEdit: () => _showTaskSheet(task),
                    onDelete: () => _confirmDelete(task),
                  ),
                ),
              ],
              if (_hasMore)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: TextButton(
                    onPressed: _loadingMore
                        ? null
                        : () => _loadTasks(loadMore: true),
                    child: Text(
                      _loadingMore ? 'Carregando...' : 'Carregar mais',
                    ),
                  ),
                ),
            ],
          ),
        ),
        if (completedGoal)
          Positioned.fill(
            child: IgnorePointer(
              child: AnimatedBuilder(
                animation: _confettiController,
                builder: (context, _) => CompositedTransformFollower(
                  link: _goalCardLink,
                  targetAnchor: Alignment.topLeft,
                  followerAnchor: Alignment.topLeft,
                  offset: const Offset(0, -20),
                  showWhenUnlinked: false,
                  child: SizedBox(
                    width: MediaQuery.sizeOf(context).width - 44,
                    height: MediaQuery.sizeOf(context).height * .65,
                    child: ConfettiAnimation(
                      progress: _confettiController.value,
                    ),
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class PomodoroPage extends StatefulWidget {
  const PomodoroPage({
    super.key,
    this.embedded = false,
    this.onSessionCompleted,
  });

  final bool embedded;
  final ValueChanged<String>? onSessionCompleted;

  @override
  State<PomodoroPage> createState() => _PomodoroPageState();
}

class _PomodoroPageState extends State<PomodoroPage> {
  final _preferencesStore = PomodoroPreferencesStore();
  int _focusMinutes = 25;
  int _shortBreakMinutes = 5;
  int _longBreakMinutes = 15;
  int _sessionsPerCycle = 4;
  _PomodoroMode _mode = _PomodoroMode.focus;
  Duration _remaining = const Duration(minutes: 25);
  Timer? _timer;
  int _sessions = 0;

  bool get _running => _timer != null;
  Duration get _total => switch (_mode) {
    _PomodoroMode.focus => Duration(minutes: _focusMinutes),
    _PomodoroMode.shortBreak => Duration(minutes: _shortBreakMinutes),
    _PomodoroMode.longBreak => Duration(minutes: _longBreakMinutes),
  };

  int get _sessionInCycle {
    if (_sessions == 0) {
      return 1;
    }

    return _mode == _PomodoroMode.focus
        ? _sessions % _sessionsPerCycle + 1
        : (_sessions - 1) % _sessionsPerCycle + 1;
  }

  @override
  void initState() {
    super.initState();
    _loadPreferences();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _loadPreferences() async {
    final preferences = await _preferencesStore.read();
    if (!mounted) {
      return;
    }

    setState(() {
      _focusMinutes = preferences.focusMinutes;
      _shortBreakMinutes = preferences.shortBreakMinutes;
      _longBreakMinutes = preferences.longBreakMinutes;
      _sessionsPerCycle = preferences.sessionsPerCycle;
      _remaining = _total;
    });
  }

  void _toggle() {
    if (_timer != null) {
      _timer?.cancel();
      setState(() => _timer = null);
      return;
    }
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (_remaining.inSeconds <= 1) {
        _timer?.cancel();
        late final String completionMessage;
        setState(() {
          _timer = null;
          if (_mode == _PomodoroMode.focus) {
            _sessions++;
            _mode = _sessions % _sessionsPerCycle == 0
                ? _PomodoroMode.longBreak
                : _PomodoroMode.shortBreak;
            completionMessage = _mode == _PomodoroMode.longBreak
                ? 'Sessão de foco concluída! Hora da pausa longa.'
                : 'Sessão de foco concluída! Hora da pausa curta.';
          } else {
            _mode = _PomodoroMode.focus;
            completionMessage = 'Pausa concluída! Hora de focar.';
          }
          _remaining = _total;
        });
        widget.onSessionCompleted?.call(completionMessage);
      } else {
        setState(() => _remaining -= const Duration(seconds: 1));
      }
    });
    setState(() {});
  }

  void _selectMode(_PomodoroMode mode) {
    _timer?.cancel();
    setState(() {
      _timer = null;
      _mode = mode;
      _remaining = _total;
    });
  }

  void _reset() {
    _timer?.cancel();
    setState(() {
      _timer = null;
      _remaining = _total;
    });
  }

  Future<void> _showSessionSettings() async {
    var focusMinutes = _focusMinutes;
    var shortBreakMinutes = _shortBreakMinutes;
    var longBreakMinutes = _longBreakMinutes;
    var sessionsPerCycle = _sessionsPerCycle;

    await showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      isScrollControlled: true,
      builder: (context) => StatefulBuilder(
        builder: (context, updateSheet) => _SheetFrame(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Sessões do Pomodoro', style: _titleStyle),
              const SizedBox(height: 8),
              const Text(
                'Ajuste as durações e a quantidade de sessões do seu ciclo.',
                style: TextStyle(color: _muted),
              ),
              const SizedBox(height: 20),
              _DurationSlider(
                label: 'Foco',
                value: focusMinutes,
                min: 5,
                max: 90,
                divisions: 17,
                onChanged: (value) =>
                    updateSheet(() => focusMinutes = value.round()),
              ),
              _DurationSlider(
                label: 'Pausa curta',
                value: shortBreakMinutes,
                min: 1,
                max: 30,
                divisions: 29,
                onChanged: (value) =>
                    updateSheet(() => shortBreakMinutes = value.round()),
              ),
              _DurationSlider(
                label: 'Pausa longa',
                value: longBreakMinutes,
                min: 5,
                max: 60,
                divisions: 11,
                onChanged: (value) =>
                    updateSheet(() => longBreakMinutes = value.round()),
              ),
              _DurationSlider(
                label: 'Sessões até a pausa longa',
                value: sessionsPerCycle,
                min: 2,
                max: 8,
                divisions: 6,
                unit: 'sessões',
                onChanged: (value) =>
                    updateSheet(() => sessionsPerCycle = value.round()),
              ),
              const SizedBox(height: 18),
              _PrimaryButton(
                label: 'Salvar ajustes',
                onPressed: () async {
                  _timer?.cancel();
                  setState(() {
                    _timer = null;
                    _focusMinutes = focusMinutes;
                    _shortBreakMinutes = shortBreakMinutes;
                    _longBreakMinutes = longBreakMinutes;
                    _sessionsPerCycle = sessionsPerCycle;
                    _remaining = _total;
                  });
                  await _preferencesStore.save(
                    PomodoroPreferences(
                      focusMinutes: focusMinutes,
                      shortBreakMinutes: shortBreakMinutes,
                      longBreakMinutes: longBreakMinutes,
                      sessionsPerCycle: sessionsPerCycle,
                    ),
                  );
                  if (context.mounted) {
                    Navigator.pop(context);
                  }
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final progress = 1 - _remaining.inSeconds / _total.inSeconds;
    final time =
        '${_remaining.inMinutes.toString().padLeft(2, '0')}:${(_remaining.inSeconds % 60).toString().padLeft(2, '0')}';
    final title = switch (_mode) {
      _PomodoroMode.focus => 'Hora de focar',
      _PomodoroMode.shortBreak => 'Pausa curta',
      _PomodoroMode.longBreak => 'Pausa longa',
    };
    final subtitle = switch (_mode) {
      _PomodoroMode.focus => 'Uma coisa de cada vez.',
      _PomodoroMode.shortBreak => 'Respire. Você está indo bem.',
      _PomodoroMode.longBreak => 'Descanse para voltar com energia.',
    };
    final content = Padding(
      padding: const EdgeInsets.fromLTRB(22, 18, 22, 28),
      child: Column(
        children: [
          Row(
            children: [
              Image.asset('assets/images/fluently_icon.png', width: 30),
              const SizedBox(width: 10),
              const Text(
                'Pomodoro',
                style: TextStyle(
                  color: _ink,
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const Spacer(),
              IconButton(
                onPressed: _showSessionSettings,
                icon: const Icon(Icons.tune_rounded),
                color: _primary,
                tooltip: 'Configurar sessões',
              ),
            ],
          ),
          const SizedBox(height: 28),
          _ModeTabs(mode: _mode, onSelect: _selectMode),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                TweenAnimationBuilder<double>(
                  tween: Tween(end: progress),
                  duration: const Duration(milliseconds: 250),
                  curve: Curves.linear,
                  builder: (context, value, _) => SizedBox(
                    width: 250,
                    height: 250,
                    child: CustomPaint(
                      painter: _TimerRingPainter(
                        progress: value,
                        active: _running,
                      ),
                      child: Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              _mode == _PomodoroMode.focus
                                  ? Icons.local_fire_department_rounded
                                  : Icons.spa_rounded,
                              color: _primary,
                              size: 28,
                            ),
                            const SizedBox(height: 8),
                            Text(
                              time,
                              style: const TextStyle(
                                color: _ink,
                                fontSize: 46,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -2,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              title,
                              style: const TextStyle(
                                color: _muted,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 24),
                Text(
                  subtitle,
                  style: const TextStyle(color: _muted, fontSize: 15),
                ),
                const SizedBox(height: 10),
                Text(
                  'Sessão $_sessionInCycle de $_sessionsPerCycle',
                  style: const TextStyle(
                    color: _primary,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(
            width: double.infinity,
            child: _PrimaryButton(
              label: _running ? 'Pausar' : 'Começar foco',
              icon: _running ? Icons.pause_rounded : Icons.play_arrow_rounded,
              onPressed: _toggle,
            ),
          ),
          const SizedBox(height: 12),
          TextButton.icon(
            onPressed: _reset,
            icon: const Icon(Icons.restart_alt_rounded),
            label: const Text('Recomeçar sessão'),
          ),
        ],
      ),
    );

    if (widget.embedded) {
      return content;
    }

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(child: content),
    );
  }
}

class _DurationSlider extends StatelessWidget {
  const _DurationSlider({
    required this.label,
    required this.value,
    required this.min,
    required this.max,
    required this.divisions,
    this.unit = 'min',
    required this.onChanged,
  });

  final String label;
  final int value;
  final int min;
  final int max;
  final int divisions;
  final String unit;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Row(
        children: [
          Text(
            label,
            style: const TextStyle(color: _ink, fontWeight: FontWeight.w700),
          ),
          const Spacer(),
          Text(
            '$value $unit',
            style: const TextStyle(
              color: _primary,
              fontWeight: FontWeight.w800,
            ),
          ),
        ],
      ),
      SliderTheme(
        data: SliderTheme.of(context).copyWith(
          activeTrackColor: _primary,
          inactiveTrackColor: _outline,
          thumbColor: Colors.white,
          overlayColor: _primary.withValues(alpha: .12),
          thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 10),
          trackHeight: 6,
        ),
        child: Slider(
          min: min.toDouble(),
          max: max.toDouble(),
          divisions: divisions,
          value: value.toDouble(),
          onChanged: onChanged,
        ),
      ),
    ],
  );
}

class _TaskCard extends StatelessWidget {
  const _TaskCard({
    required this.task,
    required this.onToggle,
    required this.onEdit,
    required this.onDelete,
  });
  final TodoTask task;
  final ValueChanged<bool> onToggle;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  @override
  Widget build(BuildContext context) {
    final priority = ['Baixa', 'Média', 'Alta'][task.priority.clamp(1, 3) - 1];
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Container(
        decoration: BoxDecoration(
          color: _surface,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: _outline),
        ),
        child: Material(
          color: Colors.transparent,
          borderRadius: BorderRadius.circular(18),
          child: ListTile(
            contentPadding: const EdgeInsets.fromLTRB(10, 7, 8, 7),
            leading: Checkbox(
              value: task.isCompleted,
              activeColor: _primary,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(5),
              ),
              onChanged: (value) => onToggle(value ?? false),
            ),
            title: Text(
              task.title,
              style: TextStyle(
                color: _ink,
                fontWeight: FontWeight.w700,
                decoration: task.isCompleted
                    ? TextDecoration.lineThrough
                    : null,
              ),
            ),
            subtitle: Text(
              '$priority${task.dueDate == null ? '' : ' · ${task.dueDate}'}',
              style: const TextStyle(color: _muted, fontSize: 12),
            ),
            trailing: PopupMenuButton<String>(
              icon: const Icon(Icons.more_horiz_rounded, color: _muted),
              color: Colors.white,
              surfaceTintColor: Colors.white,
              elevation: 8,
              shadowColor: const Color(0x260D6E52),
              constraints: const BoxConstraints(minWidth: 184),
              shape: const RoundedRectangleBorder(
                borderRadius: BorderRadius.all(Radius.circular(16)),
              ),
              offset: const Offset(-8, 8),
              onSelected: (value) => value == 'edit' ? onEdit() : onDelete(),
              itemBuilder: (context) => const [
                PopupMenuItem(
                  value: 'edit',
                  height: 48,
                  child: Row(
                    children: [
                      Icon(Icons.edit_outlined, size: 19, color: _primary),
                      SizedBox(width: 10),
                      Text('Editar tarefa'),
                    ],
                  ),
                ),
                PopupMenuDivider(height: 1),
                PopupMenuItem(
                  value: 'delete',
                  height: 48,
                  child: Row(
                    children: [
                      Icon(
                        Icons.delete_outline_rounded,
                        size: 19,
                        color: Color(0xFFE95C64),
                      ),
                      SizedBox(width: 10),
                      Text(
                        'Excluir tarefa',
                        style: TextStyle(color: Color(0xFFE95C64)),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            onTap: onEdit,
          ),
        ),
      ),
    );
  }
}

class _EmptyTasks extends StatelessWidget {
  const _EmptyTasks({required this.onAdd});
  final VoidCallback onAdd;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(28),
    decoration: BoxDecoration(
      color: _surface,
      borderRadius: BorderRadius.circular(20),
      border: Border.all(color: _outline),
    ),
    child: Column(
      children: [
        const Icon(Icons.task_alt_rounded, color: _primary, size: 34),
        const SizedBox(height: 12),
        const Text(
          'Tudo em ordem por aqui',
          style: TextStyle(color: _ink, fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 5),
        const Text(
          'Crie uma tarefa para manter seu ritmo.',
          textAlign: TextAlign.center,
          style: TextStyle(color: _muted),
        ),
        const SizedBox(height: 14),
        TextButton(onPressed: onAdd, child: const Text('Adicionar tarefa')),
      ],
    ),
  );
}

class _SheetFrame extends StatelessWidget {
  const _SheetFrame({required this.child});
  final Widget child;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.fromLTRB(20, 12, 20, 28),
    decoration: const BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
    ),
    child: SafeArea(
      top: false,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 42,
            height: 4,
            decoration: BoxDecoration(
              color: const Color(0xFFD2E2E2),
              borderRadius: BorderRadius.circular(99),
            ),
          ),
          const SizedBox(height: 18),
          child,
        ],
      ),
    ),
  );
}

class _PrimaryButton extends StatelessWidget {
  const _PrimaryButton({
    required this.label,
    required this.onPressed,
    this.icon,
    this.color = _primary,
  });
  final String label;
  final VoidCallback onPressed;
  final IconData? icon;
  final Color color;
  @override
  Widget build(BuildContext context) => FilledButton.icon(
    onPressed: onPressed,
    style: FilledButton.styleFrom(
      backgroundColor: color,
      foregroundColor: Colors.white,
      minimumSize: const Size.fromHeight(52),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.all(Radius.circular(18)),
      ),
    ),
    icon: icon == null ? null : Icon(icon),
    label: Text(label, style: const TextStyle(fontWeight: FontWeight.w800)),
  );
}

class _RoundAction extends StatelessWidget {
  const _RoundAction({required this.icon, required this.onTap});
  final IconData icon;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Material(
    color: _surface,
    borderRadius: BorderRadius.circular(14),
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: SizedBox(
        width: 42,
        height: 42,
        child: Icon(icon, color: _primary),
      ),
    ),
  );
}

class _PriorityChoice extends StatelessWidget {
  const _PriorityChoice({
    required this.value,
    required this.selected,
    required this.onTap,
  });
  final int value;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(14),
    child: AnimatedContainer(
      duration: const Duration(milliseconds: 180),
      padding: const EdgeInsets.symmetric(vertical: 13),
      decoration: BoxDecoration(
        color: selected ? _primary : _surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: selected ? _primary : _outline),
      ),
      child: Text(
        ['Baixa', 'Média', 'Alta'][value - 1],
        textAlign: TextAlign.center,
        style: TextStyle(
          color: selected ? Colors.white : _ink,
          fontSize: 12,
          fontWeight: FontWeight.w800,
        ),
      ),
    ),
  );
}

class _SelectorTile extends StatelessWidget {
  const _SelectorTile({
    required this.icon,
    required this.label,
    required this.onTap,
  });
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(16),
    child: Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: _outline),
      ),
      child: Row(
        children: [
          Icon(icon, color: _primary, size: 20),
          const SizedBox(width: 10),
          Text(
            label,
            style: const TextStyle(color: _ink, fontWeight: FontWeight.w600),
          ),
          const Spacer(),
          const Icon(Icons.chevron_right_rounded, color: _muted),
        ],
      ),
    ),
  );
}

class _ModeTabs extends StatelessWidget {
  const _ModeTabs({required this.mode, required this.onSelect});
  final _PomodoroMode mode;
  final ValueChanged<_PomodoroMode> onSelect;
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: _surface,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          for (final item in _PomodoroMode.values)
            Expanded(
              child: InkWell(
                onTap: () => onSelect(item),
                borderRadius: BorderRadius.circular(12),
                child: AnimatedContainer(
                  duration: const Duration(milliseconds: 180),
                  padding: const EdgeInsets.symmetric(vertical: 10),
                  decoration: BoxDecoration(
                    color: mode == item ? _primary : Colors.transparent,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    item.label,
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: mode == item ? Colors.white : _muted,
                      fontSize: 12,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _TimerRingPainter extends CustomPainter {
  const _TimerRingPainter({required this.progress, required this.active});
  final double progress;
  final bool active;
  @override
  void paint(Canvas canvas, Size size) {
    final center = size.center(Offset.zero);
    final radius = size.width / 2 - 13;
    final base = Paint()
      ..color = _surface
      ..style = PaintingStyle.stroke
      ..strokeWidth = 14;
    final value = Paint()
      ..color = active ? _primary : const Color(0xFF54CBB6)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 14
      ..strokeCap = StrokeCap.round;
    canvas.drawCircle(center, radius, base);
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      -math.pi / 2,
      math.max(.015, progress * math.pi * 2),
      false,
      value,
    );
  }

  @override
  bool shouldRepaint(covariant _TimerRingPainter oldDelegate) =>
      oldDelegate.progress != progress || oldDelegate.active != active;
}

enum _PomodoroMode {
  focus('Foco'),
  shortBreak('Pausa'),
  longBreak('Longa');

  const _PomodoroMode(this.label);
  final String label;
}

const _titleStyle = TextStyle(
  color: _ink,
  fontSize: 22,
  fontWeight: FontWeight.w800,
);
const _sectionStyle = TextStyle(
  color: _ink,
  fontSize: 19,
  fontWeight: FontWeight.w800,
);
String _apiDate(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';
String _dateLabel(DateTime date) =>
    '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
int _taskOrder(TodoTask first, TodoTask second) {
  if (first.isCompleted != second.isCompleted) {
    return first.isCompleted ? 1 : -1;
  }
  if (first.priority != second.priority) {
    return second.priority.compareTo(first.priority);
  }
  return (first.dueDate ?? '9999-12-31').compareTo(
    second.dueDate ?? '9999-12-31',
  );
}

String? _taskTitleValidator(String? value) {
  final title = value?.trim() ?? '';
  if (title.isEmpty) {
    return 'Informe o título da tarefa.';
  }
  if (title.length > 160) {
    return 'O título deve ter no máximo 160 caracteres.';
  }
  return null;
}
