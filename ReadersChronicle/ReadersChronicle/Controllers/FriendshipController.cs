﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Data;
using ReadersChronicle.Services;

namespace ReadersChronicle.Controllers
{
    public class FriendshipController : Controller
    {
        private readonly FriendshipService _friendshipService;
        private readonly UserManager<User> _userManager;

        public FriendshipController(FriendshipService friendshipService, UserManager<User> userManager)
        {
            _friendshipService = friendshipService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Search(string query)
        {
            var currentUserId = _userManager.GetUserId(User);
            var users = await _friendshipService.SearchUsersAsync(query, currentUserId);
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var result = await _friendshipService.SendFriendRequestAsync(currentUserId, userId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Search));
        }

        public async Task<IActionResult> SentInvites()
        {
            var currentUserId = _userManager.GetUserId(User);
            var invites = await _friendshipService.GetSentInvitesAsync(currentUserId);
            return View(invites);
        }

        public async Task<IActionResult> ReceivedInvites()
        {
            var currentUserId = _userManager.GetUserId(User);
            var invites = await _friendshipService.GetReceivedInvitesAsync(currentUserId);
            return View(invites);
        }

        [HttpPost]
        public async Task<IActionResult> CancelInvite(int friendshipId)
        {
            var currentUserId = _userManager.GetUserId(User);
            await _friendshipService.CancelSentInviteAsync(friendshipId, currentUserId);
            return RedirectToAction(nameof(SentInvites));
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int friendshipId)
        {
            var currentUserId = _userManager.GetUserId(User);
            await _friendshipService.ApproveFriendRequestAsync(friendshipId, currentUserId);
            return RedirectToAction(nameof(ReceivedInvites));
        }

        [HttpPost]
        public async Task<IActionResult> DenyRequest(int friendshipId)
        {
            var currentUserId = _userManager.GetUserId(User);
            await _friendshipService.DenyFriendRequestAsync(friendshipId, currentUserId);
            return RedirectToAction(nameof(ReceivedInvites));
        }

        public async Task<IActionResult> Friends()
        {
            var currentUserId = _userManager.GetUserId(User);
            var friends = await _friendshipService.GetFriendsAsync(currentUserId);
            return View(friends);
        }
    }

}