using System;
using System.Collections.Generic;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// In-memory REPL history with prefix aware navigation and reverse search.
    /// </summary>
    public sealed class ReplHistory
    {
        private const int DefaultCapacity = 500;

        private readonly List<string> _entries = new List<string>();
        private readonly int _capacity;

        private int _index;
        private string _prefix = string.Empty;
        private string _pending = string.Empty;
        private bool _navigating;

        public ReplHistory() : this(DefaultCapacity)
        {
        }

        public ReplHistory(int capacity)
        {
            _capacity = Math.Max(1, capacity);
            _index = 0;
        }

        public IReadOnlyList<string> Entries => _entries;

        public void Add(string entry)
        {
            ResetNavigation();

            if (string.IsNullOrWhiteSpace(entry))
            {
                return;
            }

            entry = entry.TrimEnd('\r', '\n');

            if (_entries.Count > 0 && string.Equals(_entries[_entries.Count - 1], entry, StringComparison.Ordinal))
            {
                return;
            }

            _entries.Add(entry);
            if (_entries.Count > _capacity)
            {
                _entries.RemoveAt(0);
            }

            _index = _entries.Count;
        }

        public void ResetNavigation()
        {
            _navigating = false;
            _index = _entries.Count;
            _prefix = string.Empty;
            _pending = string.Empty;
        }

        /// <summary>
        /// Moves one entry back. The text left of the caret when navigation started
        /// acts as a filter, so typing "var" and pressing Up recalls "var" submissions only.
        /// </summary>
        public bool TryGetPrevious(string current, int caret, out string entry)
        {
            if (!_navigating)
            {
                _navigating = true;
                _pending = current ?? string.Empty;
                _prefix = caret > 0 && caret <= _pending.Length ? _pending.Substring(0, caret) : string.Empty;
                _index = _entries.Count;
            }

            for (var i = _index - 1; i >= 0; i--)
            {
                if (Matches(_entries[i]))
                {
                    _index = i;
                    entry = _entries[i];
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public bool TryGetNext(out string entry)
        {
            if (!_navigating)
            {
                entry = null;
                return false;
            }

            for (var i = _index + 1; i < _entries.Count; i++)
            {
                if (Matches(_entries[i]))
                {
                    _index = i;
                    entry = _entries[i];
                    return true;
                }
            }

            // Past the newest match: restore whatever was being typed.
            _index = _entries.Count;
            entry = _pending;
            _navigating = false;
            return true;
        }

        /// <summary>
        /// Finds the newest entry at or before <paramref name="startIndex"/> containing <paramref name="term"/>.
        /// </summary>
        public bool TryReverseSearch(string term, int startIndex, out int foundIndex, out string entry)
        {
            var from = Math.Min(startIndex, _entries.Count - 1);
            for (var i = from; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(term) || _entries[i].IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundIndex = i;
                    entry = _entries[i];
                    return true;
                }
            }

            foundIndex = -1;
            entry = null;
            return false;
        }

        public int Count => _entries.Count;

        private bool Matches(string entry) =>
            _prefix.Length == 0 || entry.StartsWith(_prefix, StringComparison.Ordinal);
    }
}
