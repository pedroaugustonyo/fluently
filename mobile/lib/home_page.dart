import 'dart:async';
import 'dart:convert';
import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:image_picker/image_picker.dart';

import 'auth_api_client.dart';
import 'fluently_api_client.dart';

void showHomeSonnerToast(
  BuildContext context,
  String message, {
  bool isError = true,
}) {
  final overlay = Overlay.of(context);
  late OverlayEntry entry;
  entry = OverlayEntry(
    builder: (context) => _HomeSonnerToast(
      message: message,
      isError: isError,
      onDismissed: entry.remove,
    ),
  );
  overlay.insert(entry);
}

class _HomeSonnerToast extends StatefulWidget {
  const _HomeSonnerToast({
    required this.message,
    required this.isError,
    required this.onDismissed,
  });

  final String message;
  final bool isError;
  final VoidCallback onDismissed;

  @override
  State<_HomeSonnerToast> createState() => _HomeSonnerToastState();
}

class _HomeSonnerToastState extends State<_HomeSonnerToast>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    duration: const Duration(milliseconds: 260),
    reverseDuration: const Duration(milliseconds: 180),
    vsync: this,
  );
  late final Animation<Offset> _position = Tween<Offset>(
    begin: const Offset(0, -.2),
    end: Offset.zero,
  ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));

  @override
  void initState() {
    super.initState();
    _controller.forward();
    _dismissAfterDelay();
  }

  Future<void> _dismissAfterDelay() async {
    await Future<void>.delayed(const Duration(seconds: 3));
    if (!mounted) {
      return;
    }
    await _controller.reverse();
    if (mounted) {
      widget.onDismissed();
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final accent = widget.isError
        ? const Color(0xFFF17B7B)
        : const Color(0xFF5CE0BA);
    return SafeArea(
      child: Align(
        alignment: Alignment.topCenter,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 0),
          child: SlideTransition(
            position: _position,
            child: FadeTransition(
              opacity: _controller,
              child: Material(
                color: Colors.transparent,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xF017202B),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: const Color(0x33FFFFFF)),
                    boxShadow: const [
                      BoxShadow(
                        color: Color(0x33000000),
                        blurRadius: 24,
                        offset: Offset(0, 10),
                      ),
                    ],
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        widget.isError
                            ? Icons.error_outline_rounded
                            : Icons.check_circle_outline_rounded,
                        color: accent,
                        size: 19,
                      ),
                      const SizedBox(width: 10),
                      Flexible(
                        child: Text(
                          widget.message,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 13,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class HomePage extends StatefulWidget {
  const HomePage({super.key, required this.session, required this.onSignOut});

  final AuthSession session;
  final VoidCallback onSignOut;

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  late final FluentlyApiClient _api;
  UserProfile? _profile;
  Question? _question;
  AnswerResult? _answer;
  QuestionAlternative? _selectedAlternative;
  List<Question> _history = const [];
  bool _historyHasMore = false;
  List<LeaderboardEntry> _leaderboard = const [];
  int _tab = 0;
  bool _loading = true;
  bool _questionLoading = false;
  bool _submittingAnswer = false;
  bool _apiUnavailable = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = FluentlyApiClient(
      baseUrl: const String.fromEnvironment('API_BASE_URL'),
      accessToken: widget.session.accessToken,
      onUnauthorized: widget.onSignOut,
    );
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    try {
      final profile = await _api.getProfile();
      if (!mounted) return;
      setState(() {
        _profile = profile;
        _error = null;
        _apiUnavailable = false;
      });
      if (!profile.needsOnboarding && _question == null) {
        await _loadQuestion();
      }
      if (mounted) setState(() => _loading = false);
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (mounted) {
        setState(() {
          _error = error.message;
          _loading = false;
        });
      }
    }
  }

  bool _handleUnavailableApiError(ApiException error) {
    if (error.statusCode != null && error.statusCode! < 500) {
      return false;
    }

    if (mounted) {
      setState(() {
        _apiUnavailable = true;
        _error = error.message;
        _loading = false;
      });
    }
    return true;
  }

  Future<void> _retryApi() async {
    if (mounted) {
      setState(() {
        _apiUnavailable = false;
        _error = null;
        _loading = _profile == null;
      });
    }

    await _loadProfile();
    if (!mounted || _apiUnavailable || _profile?.needsOnboarding == true) {
      return;
    }

    if (_tab == 1) {
      await _loadLeaderboard();
    }
  }

  Future<void> _loadQuestion() async {
    if (_questionLoading) return;
    setState(() => _questionLoading = true);
    try {
      final question = await _api.getCurrentQuestion();
      if (mounted) {
        setState(() {
          _question = question;
          _answer = null;
          _selectedAlternative = null;
        });
      }
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (error.statusCode == 404) {
        await _generateQuestion();
      } else if (mounted) {
        setState(() => _error = error.message);
      }
    } finally {
      if (mounted) setState(() => _questionLoading = false);
    }
  }

  Future<void> _generateQuestion() async {
    if (mounted) setState(() => _questionLoading = true);
    try {
      final question = await _api.generateQuestion();
      if (mounted) {
        setState(() {
          _question = question;
          _answer = null;
          _selectedAlternative = null;
        });
      }
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _questionLoading = false);
    }
  }

  Future<void> _submit(QuestionAlternative alternative) async {
    if (_question == null || _answer != null || _submittingAnswer) return;
    setState(() => _submittingAnswer = true);
    try {
      final answer = await _api.submitAnswer(_question!.id, alternative.index);
      if (answer.isCorrect) {
        unawaited(_playCorrectAnswerFeedback());
      }
      if (mounted) {
        setState(() {
          _answer = answer;
          _profile = _profile?.copyWith(
            totalXp: answer.totalXp,
            currentStreak: answer.currentStreak,
          );
        });
      }
    } on ApiException catch (error) {
      if (!_handleUnavailableApiError(error) && mounted) {
        showHomeSonnerToast(context, error.message);
      }
    } finally {
      if (mounted) setState(() => _submittingAnswer = false);
    }
  }

  Future<void> _playCorrectAnswerFeedback() async {
    await HapticFeedback.heavyImpact();
    await Future<void>.delayed(const Duration(milliseconds: 70));
    await HapticFeedback.mediumImpact();
    await SystemSound.play(SystemSoundType.alert);
  }

  Future<void> _showNextQuestion() => _generateQuestion();

  Future<void> _loadHistory() async {
    try {
      final history = await _api.getQuestions();
      if (mounted) {
        setState(() {
          _history = history.items;
          _historyHasMore = history.hasMore;
        });
      }
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (mounted) setState(() => _error = error.message);
    }
  }

  Future<void> _loadLeaderboard() async {
    try {
      final leaderboard = await _api.getLeaderboard();
      if (mounted) setState(() => _leaderboard = leaderboard);
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (mounted) setState(() => _error = error.message);
    }
  }

  Future<void> _changeTab(int index) async {
    setState(() => _tab = index);
    if (index == 1) await _loadLeaderboard();
    if (index == 0) await _loadQuestion();
    if (index == 2) await _loadProfile();
  }

  Future<void> _completeOnboarding(int proficiency, String bio) async {
    try {
      final profile = await _api.updateProfile(
        proficiency: proficiency,
        bio: bio,
      );
      final question = await _api.generateQuestion();
      if (mounted) {
        setState(() {
          _profile = profile;
          _question = question;
          _answer = null;
        });
      }
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (mounted) setState(() => _error = error.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const _LoadingScreen();
    }
    if (_apiUnavailable || (_error != null && _profile == null)) {
      return _ErrorScreen(message: _error!, onRetry: _retryApi);
    }
    if (_profile!.needsOnboarding) {
      return OnboardingPage(onComplete: _completeOnboarding, error: _error);
    }

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        bottom: false,
        child: Stack(
          children: [
            Column(
              children: [
                Expanded(
                  child: _tab == 0
                      ? _QuestionsTab(
                          question: _question,
                          answer: _answer,
                          currentStreak: _profile!.currentStreak,
                          selectedAlternative: _selectedAlternative,
                          isSubmittingAnswer: _submittingAnswer,
                          onSelect: (alternative) => setState(
                            () => _selectedAlternative = alternative,
                          ),
                          onConfirm: () => _selectedAlternative == null
                              ? Future.value()
                              : _submit(_selectedAlternative!),
                          onNext: _showNextQuestion,
                          onHistory: _showHistory,
                        )
                      : _tab == 1
                      ? _RankingTab(
                          entries: _leaderboard,
                          currentUserId: _profile!.id,
                          onRefresh: _loadLeaderboard,
                        )
                      : _ProfileTab(
                          profile: _profile!,
                          api: _api,
                          onProfileChanged: (profile) =>
                              setState(() => _profile = profile),
                          onApiError: _handleUnavailableApiError,
                          onSignOut: widget.onSignOut,
                        ),
                ),
                _GlassFooter(index: _tab, onSelected: _changeTab),
              ],
            ),
            Positioned.fill(
              child: AnimatedSwitcher(
                duration: const Duration(milliseconds: 180),
                reverseDuration: const Duration(milliseconds: 140),
                switchInCurve: Curves.easeOutCubic,
                switchOutCurve: Curves.easeInCubic,
                transitionBuilder: (child, animation) =>
                    FadeTransition(opacity: animation, child: child),
                child: _questionLoading
                    ? const _QuestionLoadingOverlay(
                        key: ValueKey('question-loading'),
                      )
                    : const SizedBox(key: ValueKey('question-idle')),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openQuestion(Question item) async {
    try {
      final question = await _api.getQuestion(item.id);
      if (!mounted) return;
      await showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        backgroundColor: Colors.transparent,
        builder: (_) => _QuestionDetail(question: question),
      );
    } on ApiException catch (error) {
      if (_handleUnavailableApiError(error)) {
        return;
      }
      if (mounted) setState(() => _error = error.message);
    }
  }

  Future<void> _showHistory() async {
    await _loadHistory();
    if (!mounted || _apiUnavailable) return;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _QuestionHistory(
        items: _history,
        hasMore: _historyHasMore,
        api: _api,
        onOpen: _openQuestion,
        onApiError: _handleUnavailableApiError,
      ),
    );
  }
}

class OnboardingPage extends StatefulWidget {
  const OnboardingPage({super.key, required this.onComplete, this.error});
  final Future<void> Function(int proficiency, String bio) onComplete;
  final String? error;
  @override
  State<OnboardingPage> createState() => _OnboardingPageState();
}

class _OnboardingPageState extends State<OnboardingPage> {
  final _bio = TextEditingController();
  int? _level;
  bool _submitting = false;
  @override
  void dispose() {
    _bio.dispose();
    super.dispose();
  }

  Future<void> _continue() async {
    if (_level == null || _bio.text.trim().isEmpty) {
      setState(() {});
      return;
    }
    setState(() => _submitting = true);
    await widget.onComplete(_level!, _bio.text.trim());
    if (mounted) setState(() => _submitting = false);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: const Color(0xFFF7FCFA),
    body: SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(28, 32, 28, 28),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Image.asset(
                'assets/images/fluently_mascot_auth.png',
                width: 138,
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              'Vamos personalizar\nsua jornada.',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 28,
                height: 1.05,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
            const SizedBox(height: 10),
            const Text(
              'Conte seu nível e o que você gosta. A primeira questão será feita para você.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 15, color: Color(0xFF75839A)),
            ),
            const SizedBox(height: 28),
            const Text(
              'Seu nível de inglês',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: Color(0xFF101B31),
              ),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: List.generate(6, (index) {
                final level = index + 1;
                return ChoiceChip(
                  label: Text(_levelLabel(level)),
                  selected: _level == level,
                  onSelected: (_) => setState(() => _level = level),
                  selectedColor: const Color(0xFF0BA88B),
                  labelStyle: TextStyle(
                    color: _level == level
                        ? Colors.white
                        : const Color(0xFF101B31),
                    fontWeight: FontWeight.w700,
                  ),
                );
              }),
            ),
            if (_level == null)
              const Padding(
                padding: EdgeInsets.only(top: 6),
                child: Text(
                  'Escolha seu nível para continuar.',
                  style: TextStyle(color: Color(0xFFD94A4A), fontSize: 12),
                ),
              ),
            const SizedBox(height: 24),
            const Text(
              'Sobre você',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w700,
                color: Color(0xFF101B31),
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _bio,
              maxLines: 5,
              maxLength: 2000,
              onChanged: (_) => setState(() {}),
              decoration: _inputDecoration(
                'Ex.: Eu gosto de música, tecnologia e quero viajar para Londres.',
              ),
            ),
            if (_bio.text.trim().isEmpty)
              const Text(
                'Conte um pouco sobre você.',
                style: TextStyle(color: Color(0xFFD94A4A), fontSize: 12),
              ),
            if (widget.error != null)
              Padding(
                padding: const EdgeInsets.only(top: 10),
                child: Text(
                  widget.error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: Color(0xFFD94A4A),
                    fontSize: 13,
                  ),
                ),
              ),
            const SizedBox(height: 14),
            _ActionButton(
              label: 'Prosseguir',
              loading: _submitting,
              onPressed: _continue,
            ),
          ],
        ),
      ),
    ),
  );
}

class _QuestionsTab extends StatelessWidget {
  const _QuestionsTab({
    required this.question,
    required this.answer,
    required this.currentStreak,
    required this.selectedAlternative,
    required this.isSubmittingAnswer,
    required this.onSelect,
    required this.onConfirm,
    required this.onNext,
    required this.onHistory,
  });
  final Question? question;
  final AnswerResult? answer;
  final int currentStreak;
  final QuestionAlternative? selectedAlternative;
  final bool isSubmittingAnswer;
  final ValueChanged<QuestionAlternative> onSelect;
  final Future<void> Function() onConfirm;
  final Future<void> Function() onNext;
  final Future<void> Function() onHistory;
  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: onHistory,
    child: ListView(
      padding: const EdgeInsets.fromLTRB(18, 10, 18, 10),
      children: [
        Row(
          children: [
            Image.asset('assets/images/fluently_icon.png', width: 30),
            const SizedBox(width: 10),
            const Text(
              'Questões',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
            const Spacer(),
            if (currentStreak > 1) _StreakFire(streak: currentStreak),
            if (currentStreak > 1) const SizedBox(width: 10),
            IconButton(
              onPressed: onHistory,
              icon: const Icon(Icons.history_rounded),
              color: const Color(0xFF0BA88B),
              tooltip: 'Questões respondidas',
            ),
          ],
        ),
        const SizedBox(height: 18),
        if (question == null)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(40),
              child: CircularProgressIndicator(),
            ),
          )
        else
          _QuestionCard(
            question: question!,
            answer: answer,
            currentStreak: currentStreak,
            selectedAlternative: selectedAlternative,
            isSubmittingAnswer: isSubmittingAnswer,
            onSelect: onSelect,
            onConfirm: onConfirm,
            onNext: onNext,
          ),
      ],
    ),
  );
}

class _QuestionCard extends StatelessWidget {
  const _QuestionCard({
    required this.question,
    required this.answer,
    required this.currentStreak,
    required this.selectedAlternative,
    required this.isSubmittingAnswer,
    required this.onSelect,
    required this.onConfirm,
    required this.onNext,
  });
  final Question question;
  final AnswerResult? answer;
  final int currentStreak;
  final QuestionAlternative? selectedAlternative;
  final bool isSubmittingAnswer;
  final ValueChanged<QuestionAlternative> onSelect;
  final Future<void> Function() onConfirm;
  final Future<void> Function() onNext;
  @override
  Widget build(BuildContext context) => Stack(
    clipBehavior: Clip.none,
    children: [
      Container(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 14),
        decoration: _questionGlassDecoration(),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.bolt_rounded, color: Color(0xFF0BA88B)),
                const SizedBox(width: 5),
                Text(
                  '${answer?.awardedXp ?? question.baseXp * (currentStreak > 0 ? 2 : 1)} XP',
                  style: const TextStyle(
                    color: Color(0xFF0BA88B),
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 7),
            Text(
              question.context,
              style: const TextStyle(
                fontSize: 14,
                height: 1.35,
                color: Color(0xFF52627A),
              ),
            ),
            const SizedBox(height: 13),
            Text(
              question.question.replaceAll('?', '___'),
              style: const TextStyle(
                fontSize: 20,
                height: 1.2,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
            const SizedBox(height: 19),
            ...question.alternatives.map(
              (alternative) => Padding(
                padding: const EdgeInsets.only(bottom: 7),
                child: _AlternativeButton(
                  alternative: alternative,
                  answer: answer,
                  isSelected: selectedAlternative?.index == alternative.index,
                  onPressed: () => onSelect(alternative),
                ),
              ),
            ),
            if (answer == null && selectedAlternative != null) ...[
              const SizedBox(height: 4),
              Align(
                alignment: Alignment.center,
                child: SizedBox(
                  width: 190,
                  child: _ActionButton(
                    label: 'Confirmar resposta',
                    loading: isSubmittingAnswer,
                    onPressed: onConfirm,
                  ),
                ),
              ),
            ],
            if (answer != null) ...[
              if (answer!.isCorrect) const _ConfettiBurst(),
              const SizedBox(height: 12),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: answer!.isCorrect
                      ? const Color(0x1A0BA88B)
                      : const Color(0x1AF17B7B),
                  borderRadius: BorderRadius.circular(16),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      answer!.isCorrect
                          ? '+${answer!.awardedXp} XP! Muito bem.'
                          : 'A resposta era ${answer!.correctAlternative.text}.',
                      style: TextStyle(
                        fontWeight: FontWeight.w800,
                        color: answer!.isCorrect
                            ? const Color(0xFF078F78)
                            : const Color(0xFFB44A4A),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      answer!.questionTranslation,
                      style: const TextStyle(color: Color(0xFF52627A)),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 10),
              Align(
                alignment: Alignment.center,
                child: SizedBox(
                  width: 170,
                  child: _ActionButton(
                    label: 'Próxima questão',
                    loading: false,
                    onPressed: onNext,
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
      Positioned(
        top: -34,
        right: 6,
        child: IgnorePointer(
          child: Image.asset('assets/images/fluently_mascot.png', width: 96),
        ),
      ),
    ],
  );
}

class _AlternativeButton extends StatelessWidget {
  const _AlternativeButton({
    required this.alternative,
    required this.answer,
    required this.isSelected,
    required this.onPressed,
  });
  final QuestionAlternative alternative;
  final AnswerResult? answer;
  final bool isSelected;
  final VoidCallback onPressed;
  @override
  Widget build(BuildContext context) {
    final isCorrect = answer?.correctAlternative.index == alternative.index;
    final isWrongSelection = answer != null && isSelected && !isCorrect;
    final color = isCorrect || (isSelected && answer == null)
        ? const Color(0xFF0BA88B)
        : isWrongSelection
        ? const Color(0xFFD96A6A)
        : const Color(0xFF75839A);
    final isColored = isSelected || isCorrect;
    return OutlinedButton(
      onPressed: answer == null ? onPressed : null,
      style: OutlinedButton.styleFrom(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        alignment: Alignment.centerLeft,
        side: BorderSide(color: isColored ? color : const Color(0x220BA88B)),
        backgroundColor: isColored ? color : Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      child: Row(
        children: [
          Icon(
            answer == null
                ? Icons.radio_button_unchecked_rounded
                : isCorrect
                ? Icons.check_rounded
                : isWrongSelection
                ? Icons.close_rounded
                : Icons.circle_outlined,
            size: 19,
            color: isColored ? Colors.white : color,
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  alternative.text,
                  style: TextStyle(
                    color: isColored ? Colors.white : const Color(0xFF101B31),
                    fontSize: 14.8,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                Text(
                  alternative.translation,
                  style: TextStyle(
                    color: isColored
                        ? Colors.white.withValues(alpha: .8)
                        : const Color(0xFF75839A),
                    fontSize: 12.5,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StreakFire extends StatefulWidget {
  const _StreakFire({required this.streak});
  final int streak;
  @override
  State<_StreakFire> createState() => _StreakFireState();
}

class _StreakFireState extends State<_StreakFire>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 700),
  )..repeat(reverse: true);
  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ScaleTransition(
    scale: Tween<double>(begin: .9, end: 1.1).animate(_controller),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        const Icon(
          Icons.local_fire_department_rounded,
          size: 20,
          color: Color(0xFFFF8B22),
        ),
        Text(
          '${widget.streak}',
          style: const TextStyle(
            color: Color(0xFFFF8B22),
            fontWeight: FontWeight.w800,
          ),
        ),
      ],
    ),
  );
}

class _RankingTab extends StatelessWidget {
  const _RankingTab({
    required this.entries,
    required this.currentUserId,
    required this.onRefresh,
  });
  final List<LeaderboardEntry> entries;
  final String currentUserId;
  final Future<void> Function() onRefresh;
  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: onRefresh,
    child: ListView(
      padding: const EdgeInsets.fromLTRB(22, 18, 22, 18),
      children: [
        Row(
          children: [
            Image.asset('assets/images/fluently_icon.png', width: 30),
            const SizedBox(width: 10),
            const Text(
              'Ranking',
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
          ],
        ),
        const SizedBox(height: 18),
        if (entries.isEmpty)
          const _EmptyState(
            icon: Icons.emoji_events_outlined,
            text: 'Carregando os melhores estudantes...',
          )
        else
          ...entries.map((entry) {
            final me = entry.userId == currentUserId;
            final rankColor = _rankColor(entry.rank);
            return Container(
              margin: const EdgeInsets.only(bottom: 10),
              padding: const EdgeInsets.all(14),
              decoration: _glassDecoration(18, highlighted: me),
              child: Row(
                children: [
                  SizedBox(
                    width: 34,
                    child: entry.rank <= 3
                        ? Icon(
                            Icons.emoji_events_rounded,
                            color: rankColor,
                            size: 25,
                          )
                        : Text(
                            '#${entry.rank}',
                            style: TextStyle(
                              fontWeight: FontWeight.w800,
                              color: rankColor,
                            ),
                          ),
                  ),
                  _ProfileAvatar(
                    radius: 20,
                    name: entry.fullName,
                    profileImageBase64: entry.profileImageBase64,
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      me ? '${entry.fullName} (você)' : entry.fullName,
                      style: const TextStyle(
                        fontWeight: FontWeight.w700,
                        color: Color(0xFF101B31),
                      ),
                    ),
                  ),
                  Text(
                    '${entry.totalXp} XP',
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF75839A),
                    ),
                  ),
                ],
              ),
            );
          }),
      ],
    ),
  );
}

class _ProfileTab extends StatefulWidget {
  const _ProfileTab({
    required this.profile,
    required this.api,
    required this.onProfileChanged,
    required this.onApiError,
    required this.onSignOut,
  });
  final UserProfile profile;
  final FluentlyApiClient api;
  final ValueChanged<UserProfile> onProfileChanged;
  final bool Function(ApiException error) onApiError;
  final VoidCallback onSignOut;
  @override
  State<_ProfileTab> createState() => _ProfileTabState();
}

class _ProfileTabState extends State<_ProfileTab> {
  late final TextEditingController _first = TextEditingController(
    text: widget.profile.firstName,
  );
  late final TextEditingController _last = TextEditingController(
    text: widget.profile.lastName,
  );
  late final TextEditingController _bio = TextEditingController(
    text: widget.profile.bio,
  );
  late int? _level = widget.profile.proficiency;
  late String? _profileImageBase64 = widget.profile.profileImageBase64;
  bool _saving = false;
  bool _savingProfileImage = false;
  bool _editing = false;
  @override
  void dispose() {
    _first.dispose();
    _last.dispose();
    _bio.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_editing || _saving) {
      return;
    }

    FocusScope.of(context).unfocus();
    setState(() => _saving = true);
    try {
      widget.onProfileChanged(
        await widget.api.updateProfile(
          firstName: _first.text.trim(),
          lastName: _last.text.trim(),
          bio: _bio.text.trim(),
          proficiency: _level,
        ),
      );
      if (mounted) {
        setState(() => _editing = false);
        showHomeSonnerToast(
          context,
          'Perfil atualizado com sucesso.',
          isError: false,
        );
      }
    } on ApiException catch (error) {
      if (mounted && !widget.onApiError(error)) {
        showHomeSonnerToast(context, error.message);
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _saveProfileImage(String? imageBase64) async {
    final previousImage = _profileImageBase64;
    setState(() {
      _profileImageBase64 = imageBase64;
      _savingProfileImage = true;
    });

    try {
      final profile = await widget.api.updateProfile(
        profileImageBase64: imageBase64,
      );
      widget.onProfileChanged(profile);
      if (mounted) {
        setState(() => _profileImageBase64 = profile.profileImageBase64);
        showHomeSonnerToast(
          context,
          'Foto de perfil atualizada.',
          isError: false,
        );
      }
    } on ApiException catch (error) {
      if (mounted) {
        setState(() => _profileImageBase64 = previousImage);
        if (!widget.onApiError(error)) {
          showHomeSonnerToast(context, error.message);
        }
      }
    } finally {
      if (mounted) {
        setState(() => _savingProfileImage = false);
      }
    }
  }

  Future<void> _pickProfileImage() async {
    final source = await showModalBottomSheet<_ProfileImageSource>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) =>
          _ProfileImageSourcePicker(canRemove: _profileImageBase64 != null),
    );

    if (source == null) {
      return;
    }

    if (source == _ProfileImageSource.remove) {
      await _saveProfileImage('');
      return;
    }

    final image = await ImagePicker().pickImage(
      source: source == _ProfileImageSource.camera
          ? ImageSource.camera
          : ImageSource.gallery,
      maxWidth: 480,
      maxHeight: 480,
      imageQuality: 80,
    );
    if (image == null) {
      return;
    }

    final imageBytes = await image.readAsBytes();
    if (!mounted) {
      return;
    }
    await _saveProfileImage(base64Encode(imageBytes));
  }

  @override
  Widget build(BuildContext context) => ListView(
    keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
    padding: EdgeInsets.fromLTRB(
      18,
      10,
      18,
      MediaQuery.viewInsetsOf(context).bottom + 10,
    ),
    children: [
      Center(
        child: Stack(
          clipBehavior: Clip.none,
          children: [
            _ProfileAvatar(
              radius: 34,
              name: widget.profile.firstName,
              profileImageBase64: _profileImageBase64,
            ),
            Positioned(
              right: -4,
              bottom: -4,
              child: Material(
                color: const Color(0xFF0BA88B),
                shape: const CircleBorder(),
                child: InkWell(
                  customBorder: const CircleBorder(),
                  onTap: _savingProfileImage ? null : _pickProfileImage,
                  child: Padding(
                    padding: EdgeInsets.all(6),
                    child: _savingProfileImage
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(
                            Icons.photo_camera_outlined,
                            size: 16,
                            color: Colors.white,
                          ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: 10),
      Text(
        widget.profile.fullName,
        textAlign: TextAlign.center,
        style: const TextStyle(
          fontSize: 17,
          fontWeight: FontWeight.w800,
          color: Color(0xFF101B31),
        ),
      ),
      Text(
        '${widget.profile.totalXp} XP',
        textAlign: TextAlign.center,
        style: const TextStyle(color: Color(0xFF75839A)),
      ),
      const SizedBox(height: 16),
      Row(
        children: [
          const Expanded(
            child: Text(
              'Seu perfil',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
          ),
          IconButton(
            onPressed: _editing ? null : () => setState(() => _editing = true),
            constraints: const BoxConstraints.tightFor(width: 36, height: 36),
            padding: EdgeInsets.zero,
            icon: const Icon(Icons.edit_outlined, size: 18),
            color: const Color(0xFF078F78),
            tooltip: 'Editar perfil',
          ),
        ],
      ),
      const SizedBox(height: 6),
      TextField(
        controller: _first,
        enabled: _editing,
        style: const TextStyle(fontSize: 13.5),
        decoration: _profileInputDecoration('Primeiro nome', enabled: _editing),
      ),
      const SizedBox(height: 6),
      TextField(
        controller: _last,
        enabled: _editing,
        style: const TextStyle(fontSize: 13.5),
        decoration: _profileInputDecoration('Sobrenome', enabled: _editing),
      ),
      const SizedBox(height: 6),
      _ProficiencySelect(
        value: _level,
        enabled: _editing,
        onChanged: (level) => setState(() => _level = level),
      ),
      const SizedBox(height: 6),
      TextField(
        controller: _bio,
        enabled: _editing,
        maxLines: 4,
        maxLength: 2000,
        scrollPadding: const EdgeInsets.only(bottom: 160),
        style: const TextStyle(fontSize: 13.5),
        decoration: _profileInputDecoration('Sobre você', enabled: _editing),
      ),
      const SizedBox(height: 6),
      _ActionButton(
        label: 'Salvar perfil',
        loading: _saving,
        fontSize: 13,
        onPressed: _editing ? _save : null,
      ),
      const SizedBox(height: 8),
      _ActionButton(
        label: 'Alterar e-mail ou senha',
        loading: false,
        fontSize: 13,
        onPressed: () async => _credentialsDialog(context),
      ),
      const SizedBox(height: 8),
      _ActionButton(
        label: 'Sair da conta',
        loading: false,
        color: const Color(0xFFD94A4A),
        fontSize: 13,
        icon: Icons.logout_rounded,
        onPressed: () async => widget.onSignOut(),
      ),
    ],
  );
  Future<void> _credentialsDialog(BuildContext context) async {
    final email = TextEditingController(text: widget.profile.email);
    final password = TextEditingController();
    final confirmation = TextEditingController();
    final formKey = GlobalKey<FormState>();
    var isSaving = false;
    var obscurePassword = true;
    var obscureConfirmation = true;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (dialogContext, setDialogState) => Dialog(
          insetPadding: const EdgeInsets.symmetric(horizontal: 18),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(28),
          ),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 380),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(24, 22, 24, 24),
              child: Form(
                key: formKey,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        const Expanded(
                          child: Text(
                            'Atualizar credenciais',
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w800,
                              color: Color(0xFF101B31),
                            ),
                          ),
                        ),
                        IconButton(
                          onPressed: isSaving
                              ? null
                              : () => Navigator.of(dialogContext).pop(),
                          icon: const Icon(Icons.close_rounded),
                          color: const Color(0xFF526174),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Mantenha sua conta protegida com dados atualizados.',
                      style: TextStyle(color: Color(0xFF75839A)),
                    ),
                    const SizedBox(height: 18),
                    TextFormField(
                      controller: email,
                      keyboardType: TextInputType.emailAddress,
                      validator: _credentialEmailValidator,
                      decoration: _inputDecoration('E-mail'),
                    ),
                    const SizedBox(height: 10),
                    TextFormField(
                      controller: password,
                      obscureText: obscurePassword,
                      validator: _credentialPasswordValidator,
                      decoration: _inputDecoration('Nova senha').copyWith(
                        suffixIcon: IconButton(
                          onPressed: () => setDialogState(
                            () => obscurePassword = !obscurePassword,
                          ),
                          icon: Icon(
                            obscurePassword
                                ? Icons.visibility_off_outlined
                                : Icons.visibility_outlined,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    TextFormField(
                      controller: confirmation,
                      obscureText: obscureConfirmation,
                      validator: (value) => value != password.text
                          ? 'As senhas não coincidem.'
                          : null,
                      decoration: _inputDecoration('Confirmar nova senha')
                          .copyWith(
                            suffixIcon: IconButton(
                              onPressed: () => setDialogState(
                                () =>
                                    obscureConfirmation = !obscureConfirmation,
                              ),
                              icon: Icon(
                                obscureConfirmation
                                    ? Icons.visibility_off_outlined
                                    : Icons.visibility_outlined,
                              ),
                            ),
                          ),
                    ),
                    const SizedBox(height: 20),
                    SizedBox(
                      height: 46,
                      child: ElevatedButton(
                        onPressed: isSaving
                            ? null
                            : () async {
                                if (!formKey.currentState!.validate()) {
                                  return;
                                }
                                setDialogState(() => isSaving = true);
                                try {
                                  await widget.api.updateCredentials(
                                    email: email.text.trim(),
                                    password: password.text,
                                    passwordConfirmation: confirmation.text,
                                  );
                                  if (dialogContext.mounted) {
                                    Navigator.of(dialogContext).pop();
                                  }
                                  if (context.mounted) {
                                    showHomeSonnerToast(
                                      context,
                                      'Credenciais atualizadas com sucesso.',
                                      isError: false,
                                    );
                                  }
                                } on ApiException catch (error) {
                                  if (context.mounted &&
                                      !widget.onApiError(error)) {
                                    showHomeSonnerToast(context, error.message);
                                  }
                                  if (dialogContext.mounted) {
                                    setDialogState(() => isSaving = false);
                                  }
                                }
                              },
                        child: isSaving
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Text('Salvar alterações'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
    email.dispose();
    password.dispose();
    confirmation.dispose();
  }
}

class _ProficiencySelect extends StatelessWidget {
  const _ProficiencySelect({
    required this.value,
    required this.enabled,
    required this.onChanged,
  });

  final int? value;
  final bool enabled;
  final ValueChanged<int> onChanged;

  Future<void> _showPicker(BuildContext context) async {
    var selectedLevel = value;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => StatefulBuilder(
        builder: (context, setModalState) => SafeArea(
          top: false,
          bottom: false,
          child: Container(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 28),
            decoration: const BoxDecoration(
              color: Color(0xFFFDFEFE),
              borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 38,
                  height: 4,
                  decoration: BoxDecoration(
                    color: const Color(0xFFD7E2E4),
                    borderRadius: BorderRadius.circular(99),
                  ),
                ),
                const SizedBox(height: 18),
                Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'Nível de inglês',
                        style: TextStyle(
                          fontSize: 19,
                          fontWeight: FontWeight.w800,
                          color: Color(0xFF101B31),
                        ),
                      ),
                    ),
                    IconButton(
                      onPressed: () => Navigator.of(context).pop(),
                      icon: const Icon(Icons.close_rounded),
                      color: const Color(0xFF526174),
                      tooltip: 'Fechar',
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                ...List.generate(6, (index) {
                  final level = index + 1;
                  final selected = selectedLevel == level;
                  return Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: InkWell(
                      borderRadius: BorderRadius.circular(16),
                      onTap: () {
                        setModalState(() => selectedLevel = level);
                        onChanged(level);
                      },
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 13,
                        ),
                        decoration: BoxDecoration(
                          color: selected
                              ? const Color(0xFF0BA88B)
                              : const Color(0xFFF5FAF9),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                            color: selected
                                ? const Color(0xFF0BA88B)
                                : const Color(0x3320B79B),
                          ),
                        ),
                        child: Row(
                          children: [
                            Container(
                              width: 34,
                              height: 34,
                              alignment: Alignment.center,
                              decoration: BoxDecoration(
                                color: selected
                                    ? const Color(0x33FFFFFF)
                                    : const Color(0x140BA88B),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: Text(
                                _levelLabel(level).split(' · ').first,
                                style: TextStyle(
                                  color: selected
                                      ? Colors.white
                                      : const Color(0xFF078F78),
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                _levelLabel(level).split(' · ').last,
                                style: TextStyle(
                                  color: selected
                                      ? Colors.white
                                      : const Color(0xFF101B31),
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                            if (selected)
                              const Icon(
                                Icons.check_rounded,
                                color: Colors.white,
                              ),
                          ],
                        ),
                      ),
                    ),
                  );
                }),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) => InkWell(
    borderRadius: BorderRadius.circular(14),
    onTap: enabled ? () => _showPicker(context) : null,
    child: Ink(
      height: 46,
      padding: const EdgeInsets.symmetric(horizontal: 14),
      decoration: BoxDecoration(
        color: enabled ? const Color(0xFFF9FCFC) : const Color(0xFFEFF3F3),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: enabled ? const Color(0x3320B79B) : const Color(0xFF8B969B),
        ),
      ),
      child: Row(
        children: [
          Icon(
            Icons.school_outlined,
            size: 19,
            color: enabled ? const Color(0xFF078F78) : const Color(0xFF75839A),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              value == null ? 'Selecione seu nível' : _levelLabel(value!),
              style: TextStyle(
                color: !enabled
                    ? const Color(0xFF8B969B)
                    : value == null
                    ? const Color(0xFF75839A)
                    : const Color(0xFF101B31),
                fontSize: 13.5,
                fontWeight: value == null ? FontWeight.w500 : FontWeight.w700,
              ),
            ),
          ),
          Icon(
            Icons.keyboard_arrow_down_rounded,
            color: enabled ? const Color(0xFF526174) : const Color(0xFF75839A),
          ),
        ],
      ),
    ),
  );
}

enum _ProfileImageSource { camera, gallery, remove }

class _ProfileImageSourcePicker extends StatelessWidget {
  const _ProfileImageSourcePicker({required this.canRemove});

  final bool canRemove;

  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    bottom: false,
    child: Container(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 28),
      decoration: const BoxDecoration(
        color: Color(0xFFFDFEFE),
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 38,
            height: 4,
            decoration: BoxDecoration(
              color: const Color(0xFFD7E2E4),
              borderRadius: BorderRadius.circular(99),
            ),
          ),
          const SizedBox(height: 18),
          const Align(
            alignment: Alignment.centerLeft,
            child: Text(
              'Foto de perfil',
              style: TextStyle(
                fontSize: 19,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
          ),
          const SizedBox(height: 12),
          _ProfileImageAction(
            icon: Icons.photo_library_outlined,
            label: 'Escolher da galeria',
            onTap: () => Navigator.of(context).pop(_ProfileImageSource.gallery),
          ),
          const SizedBox(height: 8),
          _ProfileImageAction(
            icon: Icons.photo_camera_outlined,
            label: 'Tirar uma foto',
            onTap: () => Navigator.of(context).pop(_ProfileImageSource.camera),
          ),
          if (canRemove) ...[
            const SizedBox(height: 8),
            _ProfileImageAction(
              icon: Icons.delete_outline_rounded,
              label: 'Remover foto',
              destructive: true,
              onTap: () =>
                  Navigator.of(context).pop(_ProfileImageSource.remove),
            ),
          ],
        ],
      ),
    ),
  );
}

class _ProfileImageAction extends StatelessWidget {
  const _ProfileImageAction({
    required this.icon,
    required this.label,
    required this.onTap,
    this.destructive = false,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final bool destructive;

  @override
  Widget build(BuildContext context) => InkWell(
    borderRadius: BorderRadius.circular(16),
    onTap: onTap,
    child: Ink(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
      decoration: BoxDecoration(
        color: destructive ? const Color(0x0FD94A4A) : const Color(0xFFF5FAF9),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          Icon(
            icon,
            color: destructive
                ? const Color(0xFFD94A4A)
                : const Color(0xFF078F78),
          ),
          const SizedBox(width: 12),
          Text(
            label,
            style: TextStyle(
              color: destructive
                  ? const Color(0xFFD94A4A)
                  : const Color(0xFF101B31),
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    ),
  );
}

class _ProfileAvatar extends StatelessWidget {
  const _ProfileAvatar({
    required this.radius,
    required this.name,
    required this.profileImageBase64,
  });

  final double radius;
  final String name;
  final String? profileImageBase64;

  @override
  Widget build(BuildContext context) {
    final imageBytes = _decodeProfileImage(profileImageBase64);
    return CircleAvatar(
      radius: radius,
      backgroundColor: const Color(0x1A0BA88B),
      backgroundImage: imageBytes == null ? null : MemoryImage(imageBytes),
      child: imageBytes == null
          ? Text(
              name.isEmpty ? '?' : name[0].toUpperCase(),
              style: TextStyle(
                fontSize: radius - 3,
                fontWeight: FontWeight.w800,
                color: const Color(0xFF078F78),
              ),
            )
          : null,
    );
  }
}

Uint8List? _decodeProfileImage(String? profileImageBase64) {
  if (profileImageBase64 == null || profileImageBase64.isEmpty) {
    return null;
  }

  try {
    return base64Decode(profileImageBase64);
  } on FormatException {
    return null;
  }
}

Color _rankColor(int rank) => switch (rank) {
  1 => const Color(0xFFE0A300),
  2 => const Color(0xFF8F9BA8),
  3 => const Color(0xFFB46B38),
  _ => const Color(0xFF8C98A8),
};

class _GlassFooter extends StatelessWidget {
  const _GlassFooter({required this.index, required this.onSelected});
  final int index;
  final ValueChanged<int> onSelected;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(55, 10, 55, 18),
    child: ClipRRect(
      borderRadius: BorderRadius.circular(24),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
        child: Container(
          decoration: _glassDecoration(24),
          child: Row(
            children: [
              _FooterItem(
                icon: Icons.auto_stories_rounded,
                label: 'Questões',
                selected: index == 0,
                onTap: () => onSelected(0),
              ),
              _FooterItem(
                icon: Icons.emoji_events_rounded,
                label: 'Ranking',
                selected: index == 1,
                onTap: () => onSelected(1),
              ),
              _FooterItem(
                icon: Icons.person_rounded,
                label: 'Perfil',
                selected: index == 2,
                onTap: () => onSelected(2),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class _FooterItem extends StatelessWidget {
  const _FooterItem({
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Expanded(
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(24),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 7),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 21,
              color: selected
                  ? const Color(0xFF0BA88B)
                  : const Color(0xFF75839A),
            ),
            const SizedBox(height: 3),
            Text(
              label,
              style: TextStyle(
                fontSize: 11,
                fontWeight: selected ? FontWeight.w800 : FontWeight.w500,
                color: selected
                    ? const Color(0xFF0BA88B)
                    : const Color(0xFF75839A),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

class _QuestionDetail extends StatelessWidget {
  const _QuestionDetail({required this.question});
  final Question question;
  @override
  Widget build(BuildContext context) => SafeArea(
    top: false,
    bottom: false,
    child: Container(
      height: MediaQuery.sizeOf(context).height * .76,
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 44),
      decoration: const BoxDecoration(
        color: Color(0xFFFDFEFE),
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Align(
              alignment: Alignment.topRight,
              child: IconButton(
                onPressed: () => Navigator.of(context).pop(),
                icon: const Icon(Icons.close_rounded),
                color: const Color(0xFF526174),
                tooltip: 'Fechar',
              ),
            ),
            Text(
              question.question.replaceAll('?', '___'),
              style: const TextStyle(
                fontSize: 19,
                fontWeight: FontWeight.w800,
                color: Color(0xFF101B31),
              ),
            ),
            const SizedBox(height: 8),
            Text(
              question.questionTranslation ?? 'Questão pendente',
              style: const TextStyle(color: Color(0xFF75839A)),
            ),
            const SizedBox(height: 18),
            ...question.alternatives.map((alternative) {
              final wasSelected =
                  alternative.index == question.submittedAlternativeIndex;
              final isCorrect =
                  alternative.index == question.correctAlternative?.index;
              final color = isCorrect
                  ? const Color(0xFF0BA88B)
                  : wasSelected
                  ? const Color(0xFFD96A6A)
                  : const Color(0xFF75839A);
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Row(
                  children: [
                    Icon(
                      isCorrect
                          ? Icons.check_rounded
                          : wasSelected
                          ? Icons.close_rounded
                          : Icons.circle_outlined,
                      color: color,
                      size: 18,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        '${alternative.text} · ${alternative.translation}',
                        style: TextStyle(
                          color: color,
                          fontWeight: wasSelected || isCorrect
                              ? FontWeight.w700
                              : FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              );
            }),
          ],
        ),
      ),
    ),
  );
}

class _ConfettiBurst extends StatefulWidget {
  const _ConfettiBurst();

  @override
  State<_ConfettiBurst> createState() => _ConfettiBurstState();
}

class _ConfettiBurstState extends State<_ConfettiBurst>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 900),
  )..forward();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 0,
    child: AnimatedBuilder(
      animation: CurvedAnimation(parent: _controller, curve: Curves.easeOut),
      builder: (context, child) => Stack(
        clipBehavior: Clip.none,
        children: List.generate(18, (index) {
          final horizontal = (index * 47 % 260).toDouble();
          final direction = index.isEven ? -1.0 : 1.0;
          final progress = _controller.value;
          return Positioned(
            left: horizontal + (progress * 20 * direction),
            top:
                12 -
                (progress * (20 - (index % 4) * 3)) +
                (progress * progress * 20),
            child: Transform.rotate(
              angle: progress * (index.isEven ? 4 : -4),
              child: Opacity(
                opacity: (1 - progress).clamp(0, 1),
                child: Container(
                  width: 6,
                  height: 10,
                  color: [
                    const Color(0xFF0BA88B),
                    const Color(0xFFFFB22C),
                    const Color(0xFF6C8DFF),
                    const Color(0xFFF17B7B),
                  ][index % 4],
                ),
              ),
            ),
          );
        }),
      ),
    ),
  );
}

class _QuestionHistory extends StatefulWidget {
  const _QuestionHistory({
    required this.items,
    required this.hasMore,
    required this.api,
    required this.onOpen,
    required this.onApiError,
  });

  final List<Question> items;
  final bool hasMore;
  final FluentlyApiClient api;
  final Future<void> Function(Question) onOpen;
  final bool Function(ApiException error) onApiError;

  @override
  State<_QuestionHistory> createState() => _QuestionHistoryState();
}

class _QuestionHistoryState extends State<_QuestionHistory> {
  late final List<Question> _items = List.of(widget.items);
  int _page = 1;
  late bool _hasMore = widget.hasMore;
  bool _loadingMore = false;

  Future<void> _loadMore() async {
    if (_loadingMore || !_hasMore) return;
    setState(() => _loadingMore = true);
    try {
      final next = await widget.api.getQuestions(page: _page + 1);
      if (mounted) {
        setState(() {
          _page = next.page;
          _hasMore = next.hasMore;
          _items.addAll(next.items);
        });
      }
    } on ApiException catch (error) {
      if (widget.onApiError(error) && mounted) {
        Navigator.of(context).pop();
      } else if (mounted) {
        showHomeSonnerToast(context, error.message);
      }
    } finally {
      if (mounted) setState(() => _loadingMore = false);
    }
  }

  @override
  Widget build(BuildContext context) => DraggableScrollableSheet(
    initialChildSize: .82,
    minChildSize: .5,
    maxChildSize: .86,
    builder: (context, controller) => Container(
      decoration: const BoxDecoration(
        color: Color(0xFFF7FCFA),
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 20, 12, 12),
            child: Row(
              children: [
                const Expanded(
                  child: Text(
                    'Questões respondidas',
                    style: TextStyle(
                      fontSize: 18.5,
                      fontWeight: FontWeight.w800,
                      color: Color(0xFF101B31),
                    ),
                  ),
                ),
                IconButton(
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.close_rounded),
                  color: const Color(0xFF526174),
                  tooltip: 'Fechar',
                ),
              ],
            ),
          ),
          Expanded(
            child: ListView.builder(
              controller: controller,
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 64),
              itemCount: _items.length + (_hasMore ? 1 : 0),
              itemBuilder: (context, index) {
                if (_hasMore && index == _items.length) {
                  return Padding(
                    padding: const EdgeInsets.only(top: 8, bottom: 40),
                    child: TextButton(
                      onPressed: _loadingMore ? null : _loadMore,
                      child: Text(
                        _loadingMore ? 'Carregando...' : 'Carregar mais',
                      ),
                    ),
                  );
                }
                final item = _items[index];
                final isCorrect = item.isCorrect == true;
                return Container(
                  margin: const EdgeInsets.only(bottom: 10),
                  decoration: _glassDecoration(16),
                  child: ListTile(
                    onTap: () => widget.onOpen(item),
                    leading: Icon(
                      isCorrect
                          ? Icons.check_circle_rounded
                          : Icons.cancel_rounded,
                      color: isCorrect
                          ? const Color(0xFF0BA88B)
                          : const Color(0xFFD96A6A),
                    ),
                    title: Text(
                      item.question.replaceAll('?', '___'),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    subtitle: Text(
                      isCorrect
                          ? 'Correta · ${item.awardedXp ?? 0} XP'
                          : 'Para revisar',
                    ),
                    trailing: const Icon(Icons.chevron_right_rounded),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    ),
  );
}

class _LoadingScreen extends StatelessWidget {
  const _LoadingScreen();
  @override
  Widget build(BuildContext context) =>
      const Scaffold(body: Center(child: CircularProgressIndicator()));
}

class _QuestionLoadingOverlay extends StatelessWidget {
  const _QuestionLoadingOverlay({super.key});

  @override
  Widget build(BuildContext context) => AbsorbPointer(
    child: BackdropFilter(
      filter: ImageFilter.blur(sigmaX: 6, sigmaY: 6),
      child: ColoredBox(
        color: const Color(0x80FFFFFF),
        child: const Center(child: CircularProgressIndicator()),
      ),
    ),
  );
}

class _ErrorScreen extends StatelessWidget {
  const _ErrorScreen({required this.message, required this.onRetry});
  final String message;
  final Future<void> Function() onRetry;
  @override
  Widget build(BuildContext context) => Scaffold(
    body: Center(
      child: Padding(
        padding: const EdgeInsets.all(28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.cloud_off_rounded,
              size: 46,
              color: Color(0xFF75839A),
            ),
            const SizedBox(height: 14),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 14),
            _ActionButton(
              label: 'Tentar novamente',
              loading: false,
              onPressed: onRetry,
            ),
          ],
        ),
      ),
    ),
  );
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.icon, required this.text});
  final IconData icon;
  final String text;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 28),
    child: Column(
      children: [
        Icon(icon, size: 34, color: const Color(0xFF0BA88B)),
        const SizedBox(height: 8),
        Text(
          text,
          textAlign: TextAlign.center,
          style: const TextStyle(color: Color(0xFF75839A)),
        ),
      ],
    ),
  );
}

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.loading,
    required this.onPressed,
    this.color = const Color(0xFF0BA88B),
    this.fontSize = 14,
    this.icon,
  });
  final String label;
  final bool loading;
  final Color color;
  final double fontSize;
  final IconData? icon;
  final Future<void> Function()? onPressed;
  @override
  Widget build(BuildContext context) => SizedBox(
    height: 42,
    child: ElevatedButton(
      onPressed: loading ? null : onPressed,
      style: ElevatedButton.styleFrom(
        backgroundColor: color,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
      ),
      child: loading
          ? const SizedBox(
              width: 19,
              height: 19,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: Colors.white,
              ),
            )
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (icon != null) ...[
                  Icon(icon, size: 17),
                  const SizedBox(width: 7),
                ],
                Text(
                  label,
                  style: TextStyle(
                    fontSize: fontSize,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
    ),
  );
}

BoxDecoration _glassDecoration(double radius, {bool highlighted = false}) =>
    BoxDecoration(
      color: highlighted ? const Color(0xD9E6F9F2) : const Color(0xBFFFFFFF),
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(color: const Color(0x66FFFFFF)),
      boxShadow: const [
        BoxShadow(
          color: Color(0x100D6E52),
          blurRadius: 18,
          offset: Offset(0, 8),
        ),
      ],
    );
BoxDecoration _questionGlassDecoration() => BoxDecoration(
  color: const Color(0xD9FFFFFF),
  borderRadius: BorderRadius.circular(20),
  border: Border.all(color: const Color(0x99FFFFFF)),
  boxShadow: const [
    BoxShadow(color: Color(0x180D6E52), blurRadius: 26, offset: Offset(0, 12)),
  ],
);
InputDecoration _inputDecoration(String hint) => InputDecoration(
  hintText: hint,
  filled: true,
  fillColor: const Color(0xBFFFFFFF),
  contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
  border: OutlineInputBorder(
    borderRadius: BorderRadius.circular(14),
    borderSide: const BorderSide(color: Color(0x220BA88B)),
  ),
  enabledBorder: OutlineInputBorder(
    borderRadius: BorderRadius.circular(14),
    borderSide: const BorderSide(color: Color(0x220BA88B)),
  ),
);

InputDecoration _profileInputDecoration(String hint, {required bool enabled}) =>
    _inputDecoration(hint).copyWith(
      fillColor: enabled ? const Color(0xBFFFFFFF) : const Color(0xFFEFF3F3),
    );

String? _credentialEmailValidator(String? value) {
  final email = value?.trim() ?? '';
  if (email.isEmpty) {
    return 'Informe seu e-mail.';
  }
  if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
    return 'Informe um e-mail válido.';
  }
  return null;
}

String? _credentialPasswordValidator(String? value) {
  final password = value ?? '';
  if (password.isEmpty) {
    return 'Informe uma nova senha.';
  }
  if (password.length < 8 ||
      !RegExp(r'[A-Z]').hasMatch(password) ||
      !RegExp(r'[a-z]').hasMatch(password) ||
      !RegExp(r'\d').hasMatch(password)) {
    return 'Use 8+ caracteres, maiúscula, minúscula e número.';
  }
  return null;
}

String _levelLabel(int level) => [
  'A1 · Iniciante',
  'A2 · Básico',
  'B1 · Intermediário',
  'B2 · Intermediário +',
  'C1 · Avançado',
  'C2 · Fluente',
][level - 1];
